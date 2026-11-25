<?php
namespace App\Http\Middleware;
use App\Models\Site;
use Closure;
use Illuminate\Http\Request;
use Symfony\Component\HttpFoundation\Response;
class AuthenticateAgent {
 public function handle(Request $request,Closure $next): Response {
  $raw=$request->bearerToken(); if(!$raw || !preg_match('/^sw_[A-Za-z0-9_-]{40,}$/',$raw)) return response()->json(['message'=>'Unauthorized'],401);
  $site=Site::where('api_key_hash',hash('sha256',$raw))->first(); if(!$site) return response()->json(['message'=>'Unauthorized'],401);
  $request->attributes->set('monitor_site',$site); return $next($request);
 }
}
