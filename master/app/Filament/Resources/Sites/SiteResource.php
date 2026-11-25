<?php
namespace App\Filament\Resources\Sites;
use App\Filament\Resources\Sites\Pages\CreateSite;
use App\Filament\Resources\Sites\Pages\EditSite;
use App\Filament\Resources\Sites\Pages\ListSites;
use App\Models\Site;
use Filament\Actions\DeleteAction;
use Filament\Actions\EditAction;
use Filament\Forms\Components\TextInput;
use Filament\Forms\Components\TagsInput;
use Filament\Resources\Resource;
use Filament\Schemas\Schema;
use Filament\Tables\Columns\TextColumn;
use Filament\Tables\Filters\SelectFilter;
use Filament\Tables\Table;
class SiteResource extends Resource {
 protected static ?string $model=Site::class; protected static string|\BackedEnum|null $navigationIcon='heroicon-o-server-stack'; protected static ?string $navigationLabel='Monitored sites';
 public static function form(Schema $schema): Schema{return $schema->components([TextInput::make('name')->required()->maxLength(120),TextInput::make('hostname')->required()->maxLength(253)->unique(ignoreRecord:true)->helperText('DNS name or server address used to identify this site. Do not include credentials.'),TagsInput::make('monitored_processes')->label('Windows applications')->placeholder('Process name, without .exe')->default([])->rules(['array','max:6'])->nestedRecursiveRules(['string','min:1','max:64','regex:/\A[A-Za-z0-9 _.()-]+\z/'])->helperText('Optional. Add up to 6 Windows process names. Keep this list in sync with MONITOR_PROCESSES in the agent .env file.')]);}
 public static function table(Table $table): Table{return $table->columns([TextColumn::make('name')->searchable()->sortable(),TextColumn::make('hostname')->searchable(),TextColumn::make('is_online')->label('Status')->formatStateUsing(fn($state)=>$state?'Online':'Offline')->badge()->color(fn($state)=>$state?'success':'danger'),TextColumn::make('monitored_processes')->label('Applications')->formatStateUsing(fn($state)=>empty($state)?'None':implode(', ',$state)),TextColumn::make('last_seen_at')->since()->sortable(),TextColumn::make('created_at')->dateTime()->sortable()])->filters([SelectFilter::make('is_online')->options([1=>'Online',0=>'Offline'])])->recordActions([EditAction::make(),DeleteAction::make()])->defaultSort('name');}
 public static function getPages(): array{return ['index'=>ListSites::route('/'),'create'=>CreateSite::route('/create'),'edit'=>EditSite::route('/{record}/edit')];}
}
