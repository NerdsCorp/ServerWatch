<?php
namespace App\Filament\Resources\Metrics;
use App\Models\Heartbeat;
use Filament\Resources\Resource;
use Filament\Schemas\Schema;
use Filament\Tables\Columns\TextColumn;
use Filament\Tables\Filters\SelectFilter;
use Filament\Tables\Table;
class MetricResource extends Resource {
 protected static ?string $model=Heartbeat::class; protected static string|\BackedEnum|null $navigationIcon='heroicon-o-chart-bar'; protected static ?string $navigationLabel='Resource reports';
 public static function form(Schema $schema):Schema{return $schema->components([]);} public static function table(Table $table):Table{return $table->columns([TextColumn::make('received_at')->dateTime()->sortable(),TextColumn::make('site.name')->label('Site'),TextColumn::make('cpu_percent')->label('CPU')->formatStateUsing(fn($s)=>number_format($s,1).'%'),TextColumn::make('memory_used_bytes')->label('Memory used')->formatStateUsing(fn($s)=>number_format($s/1073741824,2).' GB'),TextColumn::make('memory_total_bytes')->label('Memory total')->formatStateUsing(fn($s)=>number_format($s/1073741824,2).' GB'),TextColumn::make('agent_version')->label('Agent')])->filters([SelectFilter::make('site')->relationship('site','name')])->defaultSort('received_at','desc')->recordActions([]);}
 public static function canCreate():bool{return false;} public static function getPages():array{return ['index'=>Pages\ListMetrics::route('/')];}
}
