import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface PageEvent {
  pageIndex: number;
  pageSize: number;
  length: number;
}

@Component({
  selector: 'app-paginator',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './paginator.html',
  styleUrl: './paginator.scss',
})
export class Paginator {
  @Input() length: number = 0;
  @Input() pageSize: number = 10;
  @Input() pageIndex: number = 0;
  @Input() pageSizeOptions: number[] = [5, 10, 25, 50, 100];
  @Input() showFirstLastButtons: boolean = true;

  @Output() page = new EventEmitter<PageEvent>();

  get totalPages(): number {
    return Math.ceil(this.length / this.pageSize) || 1;
  }

  get startIndex(): number {
    return this.length === 0 ? 0 : this.pageIndex * this.pageSize + 1;
  }

  get endIndex(): number {
    const end = (this.pageIndex + 1) * this.pageSize;
    return Math.min(end, this.length);
  }

  get isFirstPage(): boolean {
    return this.pageIndex === 0;
  }

  get isLastPage(): boolean {
    return this.pageIndex >= this.totalPages - 1;
  }

  get rangeLabel(): string {
    if (this.length === 0) {
      return '0 of 0';
    }
    return `${this.startIndex}-${this.endIndex} of ${this.length}`;
  }

  firstPage(): void {
    if (!this.isFirstPage) {
      this.changePage(0);
    }
  }

  previousPage(): void {
    if (!this.isFirstPage) {
      this.changePage(this.pageIndex - 1);
    }
  }

  nextPage(): void {
    if (!this.isLastPage) {
      this.changePage(this.pageIndex + 1);
    }
  }

  lastPage(): void {
    if (!this.isLastPage) {
      this.changePage(this.totalPages - 1);
    }
  }

  changePageSize(newPageSize: number): void {
    const startIndex = this.pageIndex * this.pageSize;
    const newPageIndex = Math.floor(startIndex / newPageSize);

    this.pageSize = newPageSize;
    this.pageIndex = newPageIndex;

    this.emitPageEvent();
  }

  handleKeyDown(event: KeyboardEvent, action: string): void {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();

      switch (action) {
        case 'first':
          this.firstPage();
          break;
        case 'previous':
          this.previousPage();
          break;
        case 'next':
          this.nextPage();
          break;
        case 'last':
          this.lastPage();
          break;
      }
    }
  }

  private changePage(newPageIndex: number): void {
    this.pageIndex = newPageIndex;
    this.emitPageEvent();
  }

  private emitPageEvent(): void {
    this.page.emit({
      pageIndex: this.pageIndex,
      pageSize: this.pageSize,
      length: this.length
    });
  }

  trackByIndex(index: number, item: number): number {
    return item;
  }
}
