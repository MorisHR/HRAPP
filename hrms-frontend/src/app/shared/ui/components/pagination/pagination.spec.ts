import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Pagination } from './pagination';
import { IconComponent } from '../icon/icon';

describe('Pagination', () => {
  let component: Pagination;
  let fixture: ComponentFixture<Pagination>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Pagination, IconComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(Pagination);
    component = fixture.componentInstance;
  });

  describe('Component Initialization', () => {
    it('should create', () => {
      expect(component).toBeTruthy();
    });

    it('should initialize with default values', () => {
      fixture.componentRef.setInput('totalItems', 100);
      fixture.detectChanges();

      expect(component.currentPage()).toBe(1);
      expect(component.pageSize()).toBe(25);
      expect(component.pageSizeOptions()).toEqual([10, 25, 50, 100]);
    });

    it('should accept custom page size options', () => {
      fixture.componentRef.setInput('totalItems', 100);
      fixture.componentRef.setInput('pageSizeOptions', [5, 10, 20]);
      fixture.detectChanges();

      expect(component.pageSizeOptions()).toEqual([5, 10, 20]);
    });
  });

  describe('Page Calculations', () => {
    beforeEach(() => {
      fixture.componentRef.setInput('totalItems', 100);
      component.pageSize.set(10);
      fixture.detectChanges();
    });

    it('should calculate total pages correctly', () => {
      expect(component['totalPages']()).toBe(10);
    });

    it('should calculate total pages for partial last page', () => {
      fixture.componentRef.setInput('totalItems', 95);
      fixture.detectChanges();
      expect(component['totalPages']()).toBe(10);
    });

    it('should return 1 page for empty items', () => {
      fixture.componentRef.setInput('totalItems', 0);
      fixture.detectChanges();
      expect(component['totalPages']()).toBe(1);
    });

    it('should identify first page correctly', () => {
      component.currentPage.set(1);
      expect(component['isFirstPage']()).toBe(true);

      component.currentPage.set(2);
      expect(component['isFirstPage']()).toBe(false);
    });

    it('should identify last page correctly', () => {
      component.currentPage.set(10);
      expect(component['isLastPage']()).toBe(true);

      component.currentPage.set(9);
      expect(component['isLastPage']()).toBe(false);
    });
  });

  describe('Item Range Display', () => {
    beforeEach(() => {
      fixture.componentRef.setInput('totalItems', 100);
      component.pageSize.set(10);
      fixture.detectChanges();
    });

    it('should calculate item range for first page', () => {
      component.currentPage.set(1);
      expect(component['itemRangeStart']()).toBe(1);
      expect(component['itemRangeEnd']()).toBe(10);
    });

    it('should calculate item range for middle page', () => {
      component.currentPage.set(5);
      expect(component['itemRangeStart']()).toBe(41);
      expect(component['itemRangeEnd']()).toBe(50);
    });

    it('should calculate item range for last page', () => {
      component.currentPage.set(10);
      expect(component['itemRangeStart']()).toBe(91);
      expect(component['itemRangeEnd']()).toBe(100);
    });

    it('should handle partial last page', () => {
      fixture.componentRef.setInput('totalItems', 95);
      component.currentPage.set(10);
      fixture.detectChanges();

      expect(component['itemRangeStart']()).toBe(91);
      expect(component['itemRangeEnd']()).toBe(95);
    });

    it('should return 0 for empty items', () => {
      fixture.componentRef.setInput('totalItems', 0);
      fixture.detectChanges();

      expect(component['itemRangeStart']()).toBe(0);
      expect(component['itemRangeEnd']()).toBe(0);
    });
  });

  describe('Page Navigation', () => {
    beforeEach(() => {
      fixture.componentRef.setInput('totalItems', 100);
      component.pageSize.set(10);
      fixture.detectChanges();
    });

    it('should go to first page', () => {
      component.currentPage.set(5);
      component['goToFirstPage']();
      expect(component.currentPage()).toBe(1);
    });

    it('should not go before first page', () => {
      component.currentPage.set(1);
      component['goToFirstPage']();
      expect(component.currentPage()).toBe(1);
    });

    it('should go to previous page', () => {
      component.currentPage.set(5);
      component['goToPreviousPage']();
      expect(component.currentPage()).toBe(4);
    });

    it('should not go to previous from first page', () => {
      component.currentPage.set(1);
      component['goToPreviousPage']();
      expect(component.currentPage()).toBe(1);
    });

    it('should go to next page', () => {
      component.currentPage.set(5);
      component['goToNextPage']();
      expect(component.currentPage()).toBe(6);
    });

    it('should not go beyond last page', () => {
      component.currentPage.set(10);
      component['goToNextPage']();
      expect(component.currentPage()).toBe(10);
    });

    it('should go to last page', () => {
      component.currentPage.set(1);
      component['goToLastPage']();
      expect(component.currentPage()).toBe(10);
    });

    it('should not go beyond last page when already there', () => {
      component.currentPage.set(10);
      component['goToLastPage']();
      expect(component.currentPage()).toBe(10);
    });
  });

  describe('Page Size Change', () => {
    beforeEach(() => {
      fixture.componentRef.setInput('totalItems', 100);
      component.pageSize.set(10);
      component.currentPage.set(5); // Viewing items 41-50
      fixture.detectChanges();
    });

    it('should update page size', () => {
      component['onPageSizeChange']('25');
      expect(component.pageSize()).toBe(25);
    });

    it('should maintain approximate position when changing page size', () => {
      // Currently on page 5 (items 41-50)
      component['onPageSizeChange']('25');
      // Should move to page 2 (items 26-50) to keep item 41 visible
      expect(component.currentPage()).toBe(2);
    });

    it('should handle invalid page size input', () => {
      const originalSize = component.pageSize();
      component['onPageSizeChange']('invalid');
      expect(component.pageSize()).toBe(originalSize);
    });

    it('should handle negative page size', () => {
      const originalSize = component.pageSize();
      component['onPageSizeChange']('-10');
      expect(component.pageSize()).toBe(originalSize);
    });

    it('should handle zero page size', () => {
      const originalSize = component.pageSize();
      component['onPageSizeChange']('0');
      expect(component.pageSize()).toBe(originalSize);
    });
  });

  describe('Event Emissions', () => {
    beforeEach(() => {
      fixture.componentRef.setInput('totalItems', 100);
      component.pageSize.set(10);
      fixture.detectChanges();
    });

    it('should emit pageChange when navigating to next page', (done) => {
      component.pageChange.subscribe(event => {
        expect(event.page).toBe(2);
        expect(event.size).toBe(10);
        done();
      });

      component['goToNextPage']();
    });

    it('should emit pageChange when navigating to previous page', (done) => {
      component.currentPage.set(5);

      component.pageChange.subscribe(event => {
        expect(event.page).toBe(4);
        expect(event.size).toBe(10);
        done();
      });

      component['goToPreviousPage']();
    });

    it('should emit pageChange when page size changes', (done) => {
      component.pageChange.subscribe(event => {
        expect(event.page).toBe(1);
        expect(event.size).toBe(25);
        done();
      });

      component['onPageSizeChange']('25');
    });

    it('should not emit when navigation is blocked', () => {
      component.currentPage.set(1);
      let emitted = false;

      component.pageChange.subscribe(() => {
        emitted = true;
      });

      component['goToPreviousPage']();
      expect(emitted).toBe(false);
    });
  });

  describe('UI Rendering', () => {
    beforeEach(() => {
      fixture.componentRef.setInput('totalItems', 100);
      component.pageSize.set(10);
      fixture.detectChanges();
    });

    it('should render page size selector', () => {
      const select = fixture.nativeElement.querySelector('.page-size-select');
      expect(select).toBeTruthy();
    });

    it('should render all page size options', () => {
      const options = fixture.nativeElement.querySelectorAll('.page-size-select option');
      expect(options.length).toBe(4); // [10, 25, 50, 100]
    });

    it('should render navigation buttons', () => {
      const buttons = fixture.nativeElement.querySelectorAll('.page-nav-button');
      expect(buttons.length).toBe(4); // first, prev, next, last
    });

    it('should render page indicator', () => {
      const indicator = fixture.nativeElement.querySelector('.page-indicator');
      expect(indicator).toBeTruthy();
      expect(indicator.textContent).toContain('Page');
      expect(indicator.textContent).toContain('1');
      expect(indicator.textContent).toContain('10');
    });

    it('should render items range', () => {
      const range = fixture.nativeElement.querySelector('.items-range');
      expect(range).toBeTruthy();
      expect(range.textContent).toContain('1 - 10 of 100');
    });

    it('should disable first/prev buttons on first page', () => {
      component.currentPage.set(1);
      fixture.detectChanges();

      const buttons = fixture.nativeElement.querySelectorAll('.page-nav-button');
      expect(buttons[0].disabled).toBe(true); // first
      expect(buttons[1].disabled).toBe(true); // prev
    });

    it('should disable next/last buttons on last page', () => {
      component.currentPage.set(10);
      fixture.detectChanges();

      const buttons = fixture.nativeElement.querySelectorAll('.page-nav-button');
      expect(buttons[2].disabled).toBe(true); // next
      expect(buttons[3].disabled).toBe(true); // last
    });
  });

  describe('Accessibility', () => {
    beforeEach(() => {
      fixture.componentRef.setInput('totalItems', 100);
      component.pageSize.set(10);
      fixture.detectChanges();
    });

    it('should have navigation role', () => {
      const pagination = fixture.nativeElement.querySelector('.pagination');
      expect(pagination.getAttribute('role')).toBe('navigation');
    });

    it('should have aria-label on pagination', () => {
      const pagination = fixture.nativeElement.querySelector('.pagination');
      expect(pagination.getAttribute('aria-label')).toBe('Pagination navigation');
    });

    it('should have aria-label on page size select', () => {
      const select = fixture.nativeElement.querySelector('.page-size-select');
      expect(select.getAttribute('aria-label')).toBe('Select items per page');
    });

    it('should have aria-labels on navigation buttons', () => {
      const buttons = fixture.nativeElement.querySelectorAll('.page-nav-button');
      expect(buttons[0].getAttribute('aria-label')).toBe('Go to first page');
      expect(buttons[1].getAttribute('aria-label')).toBe('Go to previous page');
      expect(buttons[2].getAttribute('aria-label')).toBe('Go to next page');
      expect(buttons[3].getAttribute('aria-label')).toBe('Go to last page');
    });

    it('should have aria-disabled on disabled buttons', () => {
      component.currentPage.set(1);
      fixture.detectChanges();

      const buttons = fixture.nativeElement.querySelectorAll('.page-nav-button');
      expect(buttons[0].getAttribute('aria-disabled')).toBe('true');
      expect(buttons[1].getAttribute('aria-disabled')).toBe('true');
    });

    it('should have aria-live on page indicator', () => {
      const indicator = fixture.nativeElement.querySelector('.page-indicator');
      expect(indicator.getAttribute('aria-live')).toBe('polite');
    });

    it('should have aria-live on items range', () => {
      const range = fixture.nativeElement.querySelector('.items-range');
      expect(range.getAttribute('aria-live')).toBe('polite');
    });

    it('should have proper label for page size select', () => {
      const label = fixture.nativeElement.querySelector('.page-size-label');
      expect(label).toBeTruthy();
      expect(label.getAttribute('for')).toBe('page-size-select');
    });
  });

  describe('Edge Cases', () => {
    it('should handle single item', () => {
      fixture.componentRef.setInput('totalItems', 1);
      component.pageSize.set(10);
      fixture.detectChanges();

      expect(component['totalPages']()).toBe(1);
      expect(component['itemRangeStart']()).toBe(1);
      expect(component['itemRangeEnd']()).toBe(1);
    });

    it('should handle exact page boundary', () => {
      fixture.componentRef.setInput('totalItems', 100);
      component.pageSize.set(10);
      fixture.detectChanges();

      expect(component['totalPages']()).toBe(10);
    });

    it('should reset to valid page when totalItems decreases', () => {
      fixture.componentRef.setInput('totalItems', 100);
      component.pageSize.set(10);
      component.currentPage.set(10);
      fixture.detectChanges();

      // Reduce total items so page 10 no longer exists
      fixture.componentRef.setInput('totalItems', 50);
      fixture.detectChanges();

      expect(component.currentPage()).toBe(5); // Should auto-adjust
    });

    it('should handle very large numbers', () => {
      fixture.componentRef.setInput('totalItems', 1000000);
      component.pageSize.set(100);
      fixture.detectChanges();

      expect(component['totalPages']()).toBe(10000);
    });
  });

  describe('Two-Way Binding', () => {
    it('should support two-way binding for currentPage', () => {
      fixture.componentRef.setInput('totalItems', 100);
      component.currentPage.set(5);
      expect(component.currentPage()).toBe(5);

      component.currentPage.set(7);
      expect(component.currentPage()).toBe(7);
    });

    it('should support two-way binding for pageSize', () => {
      fixture.componentRef.setInput('totalItems', 100);
      component.pageSize.set(25);
      expect(component.pageSize()).toBe(25);

      component.pageSize.set(50);
      expect(component.pageSize()).toBe(50);
    });
  });
});
