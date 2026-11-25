<?php
namespace App\Filament\Resources\Sites\Pages;
use App\Filament\Resources\Sites\SiteResource;
use Filament\Actions\DeleteAction;
use Filament\Resources\Pages\EditRecord;
class EditSite extends EditRecord {protected static string $resource=SiteResource::class; protected function getHeaderActions():array{return [DeleteAction::make()];} protected function mutateFormDataBeforeSave(array $data):array{unset($data['api_key_hash']);return $data;}}
