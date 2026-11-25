<?php
use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;
return new class extends Migration {
 public function up(): void {
  Schema::table('sites',function(Blueprint $t){$t->json('monitored_processes')->default('[]');});
  Schema::create('application_statuses',function(Blueprint $t){$t->id();$t->foreignId('site_id')->constrained()->cascadeOnDelete();$t->string('process_name',64);$t->boolean('is_running');$t->timestamp('checked_at')->index();$t->timestamps();$t->unique(['site_id','process_name']);});
 }
 public function down(): void {Schema::dropIfExists('application_statuses');Schema::table('sites',function(Blueprint $t){$t->dropColumn('monitored_processes');});}
};
