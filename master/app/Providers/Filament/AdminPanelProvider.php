<?php
namespace App\Providers\Filament;
use Filament\Panel;
use Filament\PanelProvider;
class AdminPanelProvider extends PanelProvider {
 public function panel(Panel $panel): Panel {return $panel->default()->id('admin')->path('admin')->login()->discoverResources(in:app_path('Filament/Resources'),for:'App\\Filament\\Resources')->discoverWidgets(in:app_path('Filament/Widgets'),for:'App\\Filament\\Widgets')->widgets([\Filament\Widgets\AccountWidget::class,\App\Filament\Widgets\MonitorOverview::class])->brandName('ServerWatch')->colors(['primary'=>\Filament\Support\Colors\Color::Blue]);}
}
