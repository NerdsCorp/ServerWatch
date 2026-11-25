<?php
namespace App\Filament\Resources\ApplicationStatuses;
use App\Filament\Resources\ApplicationStatuses\Pages\ListApplicationStatuses;
use App\Models\ApplicationStatus;
use Filament\Resources\Resource;
use Filament\Schemas\Schema;
use Filament\Tables\Columns\TextColumn;
use Filament\Tables\Filters\SelectFilter;
use Filament\Tables\Table;
class ApplicationStatusResource extends Resource {
 protected static ?string $model=ApplicationStatus::class; protected static string|\BackedEnum|null $navigationIcon='heroicon-o-cpu-chip'; protected static ?string $navigationLabel='Application checks';
 public static function form(Schema $schema):Schema{return $schema->components([]);}
 public static function table(Table $table):Table{return $table->columns([TextColumn::make('site.name')->label('Site')->searchable(),TextColumn::make('process_name')->label('Process')->searchable()->sortable(),TextColumn::make('is_running')->label('Status')->formatStateUsing(fn($state)=>$state?'Running':'Stopped')->badge()->color(fn($state)=>$state?'success':'danger'),TextColumn::make('checked_at')->label('Last checked')->since()->sortable()])->filters([SelectFilter::make('site')->relationship('site','name'),SelectFilter::make('is_running')->options([1=>'Running',0=>'Stopped'])])->defaultSort('process_name')->recordActions([]);}
 public static function canCreate():bool{return false;}
 public static function getPages():array{return ['index'=>ListApplicationStatuses::route('/')];}
}
