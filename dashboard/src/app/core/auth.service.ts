import { Injectable } from '@angular/core';

const TOKEN_KEY = 'agro_token';
const EMAIL_KEY = 'agro_email';
const PRODUCER_ID_KEY = 'agro_producer_id';

@Injectable({ providedIn: 'root' })
export class AuthService {
  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }
  getEmail(): string {
    return localStorage.getItem(EMAIL_KEY) ?? '';
  }
  getProducerId(): string | null {
    return localStorage.getItem(PRODUCER_ID_KEY);
  }
  isLoggedIn(): boolean {
    return !!this.getToken();
  }
  setSession(token: string, email: string, producerId?: string): void {
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(EMAIL_KEY, email);
    if (producerId) localStorage.setItem(PRODUCER_ID_KEY, producerId);
  }
  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(EMAIL_KEY);
    localStorage.removeItem(PRODUCER_ID_KEY);
  }
}
