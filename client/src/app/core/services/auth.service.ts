import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { LoginRequest, RegisterRequest, UserProfile } from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private base = `${environment.apiBaseUrl}/api/auth`;

  register(req: RegisterRequest) {
    return this.http.post<void>(`${this.base}/register`, req, { withCredentials: true });
  }

  login(req: LoginRequest) {
    return this.http.post<void>(`${this.base}/login`, req, { withCredentials: true });
  }

  me() {
    return this.http.get<UserProfile>(`${this.base}/me`, { withCredentials: true });
  }

  logout() {
    return this.http.post<void>(`${this.base}/logout`, {}, { withCredentials: true });
  }

  loginWithGoogle(returnUrl: string = environment.appBaseUrl + '/') {
    window.location.href = `${this.base}/external/google?returnUrl=${encodeURIComponent(
      returnUrl
    )}`;
  }

  loginWithGitHub(returnUrl: string = environment.appBaseUrl + '/') {
    window.location.href = `${this.base}/external/github?returnUrl=${encodeURIComponent(
      returnUrl
    )}`;
  }
}
