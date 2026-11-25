<?php
namespace App\Filament\Widgets;
use App\Models\Incident;
use App\Models\Heartbeat;
use App\Models\Site;
use App\Models\ApplicationStatus;
use Filament\Widgets\StatsOverviewWidget as BaseWidget;
use Filament\Widgets\StatsOverviewWidget\Stat;
class MonitorOverview extends BaseWidget {
 protected function getStats():array{return [Stat::make('Monitored sites',Site::count())->description(Site::where('is_online',true)->count().' online'),Stat::make('Offline sites',Site::where('is_online',false)->whereNotNull('last_seen_at')->count())->color('danger'),Stat::make('Apps stopped',ApplicationStatus::where('is_running',false)->count())->color('danger'),Stat::make('Heartbeats today',Heartbeat::where('received_at','>=',now()->startOfDay())->count()),Stat::make('Incidents',Incident::where('occurred_at','>=',now()->subDays(7))->count().' in the last 7 days')];}
}
