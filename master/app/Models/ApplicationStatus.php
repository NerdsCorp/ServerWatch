<?php
namespace App\Models;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Relations\BelongsTo;
class ApplicationStatus extends Model {
 protected $fillable=['site_id','process_name','is_running','checked_at'];
 protected function casts():array{return ['is_running'=>'boolean','checked_at'=>'datetime'];}
 public function site():BelongsTo{return $this->belongsTo(Site::class);}
}
