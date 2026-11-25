<?php
namespace App\Models;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
class Heartbeat extends Model { public $timestamps=false; protected $fillable=['site_id','received_at','cpu_percent','memory_used_bytes','memory_total_bytes','agent_version']; protected function casts(): array{return ['received_at'=>'datetime','cpu_percent'=>'float','memory_used_bytes'=>'integer','memory_total_bytes'=>'integer'];} public function site():BelongsTo{return $this->belongsTo(Site::class);} }
