import { inject, Injectable, signal, computed } from '@angular/core';
import { catchError, finalize, of, tap } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { UserProfile } from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private auth = inject(AuthService);

  private _user = signal<UserProfile | null>(null);
  private _loading = signal(false);

  user = this._user.asReadonly();
  loading = this._loading.asReadonly();

  isAuthenticated = computed(() => this._user() !== null);

  hydrate() {
    this._loading.set(true);
    return this.auth.me().pipe(
      tap((profile) => this._user.set(profile)),
      catchError(() => {
        this._user.set(null);
        return of(null);
      }),
      finalize(() => this._loading.set(false))
    );
  }

  clear() {
    this._user.set(null);
  }

  setUser(user: UserProfile) {
    this._user.set(user);
  }
}
