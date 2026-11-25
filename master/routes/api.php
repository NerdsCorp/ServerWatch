<?php
use App\Http\Controllers\Api\HeartbeatController;
use Illuminate\Support\Facades\Route;
Route::middleware(['throttle:120,1', 'agent.key'])->post('/v1/heartbeat', HeartbeatController::class);
