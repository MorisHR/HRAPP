import { Component, Input, Output, EventEmitter, ContentChildren, QueryList, AfterContentInit } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface Step {
  label: string;
  completed?: boolean;
  optional?: boolean;
  error?: boolean;
}

@Component({
  selector: 'app-stepper',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './stepper.html',
  styleUrl: './stepper.scss',
})
export class Stepper {
  @Input() steps: Step[] = [];
  @Input() currentStep: number = 0;
  @Input() linear: boolean = false;
  @Input() orientation: 'horizontal' | 'vertical' = 'horizontal';
  @Output() stepChange = new EventEmitter<number>();

  get canGoNext(): boolean {
    return this.currentStep < this.steps.length - 1;
  }

  get canGoPrevious(): boolean {
    return this.currentStep > 0;
  }

  isStepClickable(stepIndex: number): boolean {
    if (!this.linear) {
      return true;
    }
    // In linear mode, can only click on current, previous, or completed steps
    if (stepIndex <= this.currentStep) {
      return true;
    }
    // Check if all previous steps are completed
    for (let i = 0; i < stepIndex; i++) {
      if (!this.steps[i].completed && !this.steps[i].optional) {
        return false;
      }
    }
    return true;
  }

  goToStep(stepIndex: number): void {
    if (this.isStepClickable(stepIndex)) {
      this.currentStep = stepIndex;
      this.stepChange.emit(stepIndex);
    }
  }

  next(): void {
    if (this.canGoNext) {
      if (!this.linear || this.canProceedToNextStep()) {
        this.currentStep++;
        this.stepChange.emit(this.currentStep);
      }
    }
  }

  previous(): void {
    if (this.canGoPrevious) {
      this.currentStep--;
      this.stepChange.emit(this.currentStep);
    }
  }

  canProceedToNextStep(): boolean {
    const currentStepData = this.steps[this.currentStep];
    // In linear mode, current step must be completed or optional to proceed
    return currentStepData?.completed || currentStepData?.optional || false;
  }

  getStepState(stepIndex: number): 'completed' | 'current' | 'error' | 'upcoming' {
    const step = this.steps[stepIndex];
    if (step.error) {
      return 'error';
    }
    if (step.completed) {
      return 'completed';
    }
    if (stepIndex === this.currentStep) {
      return 'current';
    }
    return 'upcoming';
  }

  isStepActive(stepIndex: number): boolean {
    return stepIndex === this.currentStep;
  }

  getStepNumber(stepIndex: number): string | number {
    const step = this.steps[stepIndex];
    if (step.error) {
      return '!';
    }
    if (step.completed) {
      return '✓';
    }
    return stepIndex + 1;
  }
}
