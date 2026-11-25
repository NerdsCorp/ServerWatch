<?php
namespace App\Services;
use App\Models\Incident;
interface NotificationConnector { public function send(Incident $incident): void; }
