<?php
return ['offline_after_seconds' => (int) env('MONITOR_OFFLINE_AFTER_SECONDS', 180), 'webhook_url' => env('MONITOR_NOTIFICATION_WEBHOOK_URL')];
