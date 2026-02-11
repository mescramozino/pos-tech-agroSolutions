import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { AuthService } from '../core/auth.service';

const BASE = '/api/identity';

export interface LoginRequest {
  email: string;
  password: string;
}
export interface RegisterRequest extends LoginRequest {}
export interface AuthResponse {
  token: string;
  email: string;
}

@Injectable({ providedIn: 'root' })
export class IdentityService {
  constructor(private http: HttpClient, private auth: AuthService) {}

  login(body: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${BASE}/api/auth/login`, body).pipe(
      tap((res) => this.auth.setSession(res.token, res.email))
    );
  }

  register(body: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${BASE}/api/auth/register`, body).pipe(
      tap((res) => this.auth.setSession(res.token, res.email))
    );
  }
}
