import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize, switchMap } from 'rxjs/operators';
import { AuthService } from '../../../../core/services/auth.service';
import { AuthStore } from '../../../../core/stores/auth.store';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
})
export class Register {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private store = inject(AuthStore);
  private router = inject(Router);

  loading = false;
  error: string | null = null;

  form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  submit() {
    if (this.loading) return;

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error = 'Please fill out the form correctly.';
      return;
    }

    this.error = null;

    this.loading = true;

    this.auth
      .register(this.form.getRawValue())
      .pipe(
        switchMap(() => this.store.hydrate()),
        finalize(() => (this.loading = false))
      )
      .subscribe({
        next: () => this.router.navigate(['/']),
        error: (err: any) => {
          const errors = err?.error?.errors;
          this.error = Array.isArray(errors)
            ? errors.join(' ')
            : 'Registration failed. Try a different email.';
        },
      });
  }

  google() {
    this.auth.loginWithGoogle();
  }
  github() {
    this.auth.loginWithGitHub();
  }
}
