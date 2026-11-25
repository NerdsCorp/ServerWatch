<?php
namespace App\Filament\Resources\Incidents;
use App\Models\Incident;
use Filament\Resources\Resource;
use Filament\Schemas\Schema;
use Filament\Tables\Columns\TextColumn;
use Filament\Tables\Table;
class IncidentResource extends Resource {
 protected static ?string $model=Incident::class; protected static string|\BackedEnum|null $navigationIcon='heroicon-o-bell-alert'; protected static ?string $navigationLabel='Incident log';
 public static function form(Schema $schema):Schema{return $schema->components([]);} public static function table(Table $table):Table{return $table->columns([TextColumn::make('occurred_at')->dateTime()->sortable(),TextColumn::make('site.name')->label('Site')->searchable(),TextColumn::make('type')->badge()->color(fn($state)=>$state==='app_recovered'?'success':'danger'),TextColumn::make('message')->wrap()])->defaultSort('occurred_at','desc')->recordActions([]);}
 public static function canCreate():bool{return false;} public static function getPages():array{return ['index'=>Pages\ListIncidents::route('/')];}
}
