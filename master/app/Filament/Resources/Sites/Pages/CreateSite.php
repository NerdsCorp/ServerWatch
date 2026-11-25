<?php
namespace App\Filament\Resources\Sites\Pages;
use App\Filament\Resources\Sites\SiteResource;
use Filament\Notifications\Notification;
use Filament\Resources\Pages\CreateRecord;
use Illuminate\Support\Str;
class CreateSite extends CreateRecord {
 protected static string $resource=SiteResource::class; private string $issuedKey='';
 protected function mutateFormDataBeforeCreate(array $data):array{$this->issuedKey='sw_'.Str::random(48);$data['api_key_hash']=hash('sha256',$this->issuedKey);$data['is_online']=false;return $data;}
 protected function afterCreate():void {Notification::make()->title('Site created — save its agent key now')->body("This key is shown once. Put it in the client's protected .env file:\nMONITOR_MASTER_URL=".rtrim(config('app.url'),'/')."\nMONITOR_SITE_KEY={$this->issuedKey}\nMONITOR_INTERVAL_SECONDS=30\nMONITOR_PROCESSES=".implode(',', $this->record->monitored_processes ?? []))->persistent()->success()->send();}
}
