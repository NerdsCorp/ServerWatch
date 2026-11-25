<?php
namespace App\Models;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
use Illuminate\Database\Eloquent\Model;
class Incident extends Model { protected $fillable=['site_id','type','message','occurred_at']; protected function casts(): array{return ['occurred_at'=>'datetime'];} public function site():BelongsTo{return $this->belongsTo(Site::class);} }
