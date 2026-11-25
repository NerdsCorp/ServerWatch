<?php
namespace App\Console\Commands;
use App\Models\Heartbeat;
use Illuminate\Console\Command;
class ExportMetrics extends Command {
 protected $signature='monitor:export-metrics {--from= : Start time, e.g. 2026-10-01} {--to= : End time} {--site= : Site ID} {--output= : Output CSV path (defaults to stdout)}';
 protected $description='Export stored CPU and memory samples to CSV.';
 public function handle():int {
  $query=Heartbeat::query()->with('site')->orderBy('received_at');
  if($this->option('from'))$query->where('received_at','>=',$this->option('from'));
  if($this->option('to'))$query->where('received_at','<=',$this->option('to'));
  if($this->option('site'))$query->where('site_id',(int)$this->option('site'));
  $path=$this->option('output'); $stream=$path?fopen($path,'wb'):fopen('php://stdout','wb'); if(!$stream){$this->error('Could not open CSV output.');return self::FAILURE;}
  fputcsv($stream,['received_at_utc','site_id','site','hostname','cpu_percent','memory_used_bytes','memory_total_bytes','memory_used_percent','agent_version']);
  $query->chunk(1000,function($samples)use($stream){foreach($samples as $sample){$site=$sample->site;fputcsv($stream,[$sample->received_at->toIso8601String(),$sample->site_id,$site?->name,$site?->hostname,$sample->cpu_percent,$sample->memory_used_bytes,$sample->memory_total_bytes,$sample->memory_total_bytes?round($sample->memory_used_bytes/$sample->memory_total_bytes*100,2):null,$sample->agent_version]);}});
  if($path){fclose($stream);$this->info('CSV report written to '.$path);} return self::SUCCESS;
 }
}
