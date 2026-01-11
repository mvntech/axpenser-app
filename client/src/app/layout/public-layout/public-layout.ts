import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Header } from '../../features/components/landing-page/header/header';
import { Footer } from '../../features/components/landing-page/footer/footer';

@Component({
  selector: 'app-public-layout',
  standalone: true,
  imports: [RouterOutlet, Header, Footer],
  templateUrl: './public-layout.html',
})
export class PublicLayout {}
