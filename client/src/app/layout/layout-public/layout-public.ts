import { Component } from '@angular/core';
import { Header } from '../../features/components/header/header';
import { Footer } from '../../features/components/footer/footer';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-layout-public',
  standalone: true,
  imports: [RouterOutlet, Header, Footer],
  templateUrl: './layout-public.html',
})
export class LayoutPublic {}
