<?php
namespace App\Console\Commands;
use App\Models\Incident;
use App\Models\Site;
use App\Services\WebhookNotificationConnector;
use Illuminate\Console\Command;
class CheckOfflineSites extends Command {
 protected $signature='monitor:check-offline'; protected $description='Record transitions for monitored sites that stop reporting.';
 public function handle(WebhookNotificationConnector $notifier): int {
  $cutoff=now('UTC')->subSeconds(config('monitor.offline_after_seconds'));
  Site::where('is_online',true)->where(function($q)use($cutoff){$q->whereNull('last_seen_at')->orWhere('last_seen_at','<',$cutoff);})->chunkById(100,function($sites)use($notifier){foreach($sites as $site){$incident=Incident::create(['site_id'=>$site->id,'type'=>'offline','message'=>$site->name.' stopped reporting heartbeats.','occurred_at'=>now('UTC')]);$site->is_online=false;$site->last_offline_notified_at=now('UTC');$site->save();try{$notifier->send($incident);}catch(\Throwable $e){report($e);}}});
  return self::SUCCESS;
 }
}
