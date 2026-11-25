<?php
namespace App\Models;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\HasMany;
class Site extends Model {
 protected $fillable=['name','hostname','api_key_hash','is_online','last_seen_at','last_offline_notified_at','monitored_processes']; protected function casts(): array{return ['is_online'=>'boolean','last_seen_at'=>'datetime','last_offline_notified_at'=>'datetime','monitored_processes'=>'array'];}
 public function setMonitoredProcessesAttribute($processes):void{$clean=[];$seen=[];foreach(is_array($processes)?$processes:[] as $process){if(!is_string($process))continue;$process=trim(preg_replace('/\.exe$/i','',$process));$key=strtolower($process);if($process===''||isset($seen[$key]))continue;$seen[$key]=true;$clean[]=$process;}$this->attributes['monitored_processes']=json_encode($clean,JSON_THROW_ON_ERROR);}
 public function heartbeats(): HasMany{return $this->hasMany(Heartbeat::class);} public function incidents(): HasMany{return $this->hasMany(Incident::class);} public function applicationStatuses(): HasMany{return $this->hasMany(ApplicationStatus::class);}
}
