<?php
namespace App\Services;
use App\Models\Incident;
use Illuminate\Support\Facades\Http;
class WebhookNotificationConnector implements NotificationConnector {
 public function send(Incident $incident): void { $url=config('monitor.webhook_url'); if(!$url) return; Http::timeout(5)->retry(2,200)->post($url,['event'=>'site.'.$incident->type,'site'=>$incident->site->name,'hostname'=>$incident->site->hostname,'message'=>$incident->message,'text'=>$incident->message,'occurred_at'=>$incident->occurred_at->toIso8601String()])->throw(); }
}
