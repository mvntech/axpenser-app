import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { AuthStore } from './core/stores/auth.stores';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  private authStore = inject(AuthStore);
  private router = inject(Router);

  ngOnInit() {
    const path = window.location.pathname;
    if (!path.startsWith('/auth')) {
      this.authStore.hydrate().subscribe();
    }
  }
}
