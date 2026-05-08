import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MedicalRecord } from './medical-record';

describe('MedicalRecord', () => {
  let component: MedicalRecord;
  let fixture: ComponentFixture<MedicalRecord>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MedicalRecord],
    }).compileComponents();

    fixture = TestBed.createComponent(MedicalRecord);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
