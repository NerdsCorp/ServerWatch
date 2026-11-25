<?php
use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;
return new class extends Migration {
 public function up(): void {
  Schema::create('sites',function(Blueprint $t){$t->id();$t->string('name');$t->string('hostname')->unique();$t->string('api_key_hash',64)->unique();$t->boolean('is_online')->default(false);$t->timestamp('last_seen_at')->nullable()->index();$t->timestamp('last_offline_notified_at')->nullable();$t->timestamps();});
  Schema::create('heartbeats',function(Blueprint $t){$t->id();$t->foreignId('site_id')->constrained()->cascadeOnDelete();$t->timestamp('received_at')->index();$t->float('cpu_percent');$t->unsignedBigInteger('memory_used_bytes');$t->unsignedBigInteger('memory_total_bytes');$t->string('agent_version',32)->nullable();$t->index(['site_id','received_at']);});
  Schema::create('incidents',function(Blueprint $t){$t->id();$t->foreignId('site_id')->constrained()->cascadeOnDelete();$t->string('type',32);$t->text('message');$t->timestamp('occurred_at')->index();$t->timestamps();$t->index(['site_id','occurred_at']);});
 } public function down(): void {Schema::dropIfExists('incidents');Schema::dropIfExists('heartbeats');Schema::dropIfExists('sites');}
};
