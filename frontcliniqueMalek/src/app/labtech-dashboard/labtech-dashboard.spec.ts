import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LabtechDashboard } from './labtech-dashboard';

describe('LabtechDashboard', () => {
  let component: LabtechDashboard;
  let fixture: ComponentFixture<LabtechDashboard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LabtechDashboard],
    }).compileComponents();

    fixture = TestBed.createComponent(LabtechDashboard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
