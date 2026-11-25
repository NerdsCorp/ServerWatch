<?php
namespace App\Http\Controllers\Api;
use App\Http\Controllers\Controller;
use App\Models\Heartbeat;
use App\Models\ApplicationStatus;
use App\Models\Incident;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Log;
use App\Services\WebhookNotificationConnector;
class HeartbeatController extends Controller {
 public function __invoke(Request $request,WebhookNotificationConnector $notifier): JsonResponse {
  $data=$request->validate(['cpu_percent'=>['required','numeric','between:0,100'],'memory_used_bytes'=>['required','integer','min:0'],'memory_total_bytes'=>['required','integer','min:1'],'agent_version'=>['nullable','string','max:32'],'processes'=>['required','array','max:6'],'processes.*'=>['required','array:name,running'],'processes.*.name'=>['required','string','min:1','max:64','regex:/\A[A-Za-z0-9 _.()-]+\z/'],'processes.*.running'=>['required','boolean']]);
  if($data['memory_used_bytes']>$data['memory_total_bytes']) return response()->json(['message'=>'Invalid memory values'],422);
  $site=$request->attributes->get('monitor_site'); $now=now('UTC');
  $reported=array_map(fn($process)=>strtolower($process['name']),$data['processes']);
  if(count($reported)!==count(array_unique($reported))) return response()->json(['message'=>'Duplicate process names are not allowed'],422);
  $expected=array_map(fn($name)=>strtolower(preg_replace('/\.exe$/i','',$name)),$site->monitored_processes ?? []); sort($reported); sort($expected);
  if($reported!==$expected) return response()->json(['message'=>'Reported processes do not match this site configuration'],422);
  $incidents=DB::transaction(function()use($site,$data,$now){
   Heartbeat::create(['site_id'=>$site->id,'received_at'=>$now,'cpu_percent'=>$data['cpu_percent'],'memory_used_bytes'=>$data['memory_used_bytes'],'memory_total_bytes'=>$data['memory_total_bytes'],'agent_version'=>$data['agent_version']??null]);
   $site->is_online=true;$site->last_seen_at=$now;$site->save();$created=[];
   $reportedNames=array_map(fn($p)=>strtolower($p['name']),$data['processes']);
   $site->applicationStatuses()->when($reportedNames!==[],fn($q)=>$q->whereNotIn(DB::raw('lower(process_name)'),$reportedNames),fn($q)=>$q)->delete();
   foreach($data['processes'] as $process){
    $status=$site->applicationStatuses()->whereRaw('lower(process_name) = ?', [strtolower($process['name'])])->first();
    if(!$status)$status=new ApplicationStatus(['site_id'=>$site->id,'process_name'=>$process['name']]);
    $known=$status->exists;$wasRunning=$status->is_running;$isRunning=(bool)$process['running'];
    if(!$isRunning&&(!$known||$wasRunning))$created[]=Incident::create(['site_id'=>$site->id,'type'=>'app_stopped','message'=>$process['name'].' is not running.','occurred_at'=>$now]);
    if($isRunning&&$known&&!$wasRunning)$created[]=Incident::create(['site_id'=>$site->id,'type'=>'app_recovered','message'=>$process['name'].' is running again.','occurred_at'=>$now]);
    $status->is_running=$isRunning;$status->checked_at=$now;$status->save();
   }
   return $created;
  });
  foreach($incidents as $incident){try{$notifier->send($incident);}catch(\Throwable $e){Log::warning('Application status notification failed',['incident_id'=>$incident->id,'reason'=>$e->getMessage()]);}}
  return response()->json(['accepted'=>true,'server_time'=>$now->toIso8601String()]);
 }
}
