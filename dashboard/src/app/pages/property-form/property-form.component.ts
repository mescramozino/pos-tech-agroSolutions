import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PropertiesService } from '../../services/properties.service';

@Component({
  selector: 'app-property-form',
  standalone: true,
  imports: [RouterLink, FormsModule],
  templateUrl: './property-form.component.html',
  styleUrls: ['./property-form.component.css'],
})
export class PropertyFormComponent {
  name = '';
  location = '';
  loading = false;
  error = '';

  constructor(
    private props: PropertiesService,
    private router: Router,
  ) {}

  submit() {
    this.error = '';
    this.loading = true;
    this.props.createProperty({ name: this.name, location: this.location || null }).subscribe({
      next: (p) => this.router.navigate(['/properties', p.id]),
      error: () => {
        this.loading = false;
        this.error = 'Erro ao criar propriedade.';
      },
      complete: () => (this.loading = false),
    });
  }
}
