import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

const BASE = '/api/identity/api/producers';

export interface User {
  id: string;
  email: string;
  createdAt: string;
}

export interface CreateUserBody {
  email: string;
  password: string;
}

export interface UpdateUserBody {
  email: string;
}

@Injectable({ providedIn: 'root' })
export class UsersService {
  constructor(private http: HttpClient) {}

  getUsers(): Observable<User[]> {
    return this.http.get<User[]>(BASE);
  }

  getUser(id: string): Observable<User> {
    return this.http.get<User>(`${BASE}/${id}`);
  }

  createUser(body: CreateUserBody): Observable<User> {
    return this.http.post<User>(BASE, body);
  }

  updateUser(id: string, body: UpdateUserBody): Observable<void> {
    return this.http.put<void>(`${BASE}/${id}`, body);
  }

  deleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`${BASE}/${id}`);
  }
}
