import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function isbn13Validator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const v = (control.value ?? '').toString();
    if (!/^\d{13}$/.test(v)) return { isbn13: 'ISBN must be 13 digits' };
    let sum = 0;
    for (let i = 0; i < 12; i++) {
      const d = v.charCodeAt(i) - 48;
      sum += (i % 2 === 0) ? d : (d * 3);
    }
    const check = (10 - (sum % 10)) % 10;
    if (check !== (v.charCodeAt(12) - 48)) return { isbn13: 'Invalid ISBN-13 checksum' };
    return null;
  };
}
