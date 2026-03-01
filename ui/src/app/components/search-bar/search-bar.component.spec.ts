import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { SearchBarComponent } from './search-bar.component';
import { By } from '@angular/platform-browser';

describe('SearchBarComponent', () => {
  let fixture: ComponentFixture<SearchBarComponent>;
  let component: SearchBarComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SearchBarComponent]
    }).compileComponents();

    fixture   = TestBed.createComponent(SearchBarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialise searchValue from @Input initialValue', () => {
    component.initialValue = 'laptop';
    component.ngOnInit();
    fixture.detectChanges();
    expect(component.searchValue).toBe('laptop');
  });

  it('should emit searchChange after debounce when input changes', fakeAsync(() => {
    const emitted: string[] = [];
    component.searchChange.subscribe(v => emitted.push(v));

    component.onInputChange('lap');
    tick(299);                    // before debounce window
    expect(emitted.length).toBe(0);

    tick(1);                      // complete 300ms debounce
    expect(emitted.length).toBe(1);
    expect(emitted[0]).toBe('lap');
  }));

  it('should not emit duplicate values (distinctUntilChanged)', fakeAsync(() => {
    const emitted: string[] = [];
    component.searchChange.subscribe(v => emitted.push(v));

    component.onInputChange('lap');
    tick(300);
    component.onInputChange('lap');   // same value again
    tick(300);

    expect(emitted.length).toBe(1);  // only one emission
  }));

  it('clear() should reset searchValue and emit empty string', fakeAsync(() => {
    const emitted: string[] = [];
    component.searchChange.subscribe(v => emitted.push(v));

    component.onInputChange('laptop');
    tick(300);

    component.clear();
    tick(300);

    expect(component.searchValue).toBe('');
    expect(emitted).toEqual(['laptop', '']);
  }));

  it('should show clear button only when searchValue is non-empty', () => {
    component.searchValue = '';
    fixture.detectChanges();
    expect(fixture.debugElement.query(By.css('.clear-btn'))).toBeNull();

    component.searchValue = 'laptop';
    fixture.detectChanges();
    expect(fixture.debugElement.query(By.css('.clear-btn'))).toBeTruthy();
  });

  it('should call clear() when Escape key is pressed on input', fakeAsync(() => {
    const emitted: string[] = [];
    component.searchChange.subscribe(v => emitted.push(v));
    component.onInputChange('laptop');
    tick(300);

    const input = fixture.debugElement.query(By.css('.search-input')).nativeElement;
    input.dispatchEvent(new KeyboardEvent('keyup', { key: 'Escape' }));
    tick(300);

    expect(component.searchValue).toBe('');
  }));
});
