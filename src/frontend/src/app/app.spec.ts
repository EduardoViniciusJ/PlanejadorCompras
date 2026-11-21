import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  afterEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.removeAttribute('data-app-theme');
    document.documentElement.style.colorScheme = '';
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);

    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should apply the theme on startup for both DaisyUI and the app variables', async () => {
    localStorage.setItem('planejador-theme', 'dark');

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(document.documentElement.getAttribute('data-theme')).toBe('claude-dark');
    expect(document.documentElement.getAttribute('data-app-theme')).toBe('dark');
  });
});
