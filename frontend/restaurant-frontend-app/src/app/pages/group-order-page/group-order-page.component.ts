import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';

import { ParticipantCardComponent } from '@app/components/group-orders/participant-card/participant-card.component';
import { Participant } from '@app/models/group-orders/participant.model';
import { GuestInvite } from '@app/models/reservations/guest-invite.model';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { consolidatePreOrder } from '@app/shared/helpers/consolidate-pre-order';
import { ModalComponent } from '@app/shared/custom-components/modal/modal.component';
import { ToastService } from '@app/services/toast/toast.service';

@Component({
  selector: 'app-group-order-page',
  imports: [ParticipantCardComponent, DatePipe, CurrencyPipe, ModalComponent],
  templateUrl: './group-order-page.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './group-order-page.component.scss',
})
export class GroupOrderPageComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly groupOrdersService = inject(GroupOrdersService);
  private readonly toastService = inject(ToastService);

  readonly invite = signal<GuestInvite | null>(null);
  readonly participants = signal<Participant[]>([]);
  readonly consolidatedOrder = computed(() =>
    consolidatePreOrder(this.participants()),
  );

  readonly pendingParticipants = computed(() =>
    this.participants().filter((person) => !person.isSubmitted),
  );

  readonly hasSubmittedDishes = computed(() =>
    this.consolidatedOrder().items.length > 0,
  );

  readonly total = computed(() => this.consolidatedOrder().total);
  readonly guestSessionId = signal('');
  readonly status = signal<'Open' | 'Submitted' | 'Closed'>('Open');

  readonly isLoading = signal(true);
  readonly error = signal(false);
  readonly isModalOpen = signal(false);
  readonly isSubmitting = signal(false);
  readonly isReadOnly = signal(false);

  constructor() {
    this.loadData();
  }

  loadData(): void {
    const reservationId = this.route.snapshot.paramMap.get('reservationId');

    if (!reservationId) {
      this.error.set(true);
      this.isLoading.set(false);
      return;
    }

    this.isLoading.set(true);
    this.error.set(false);

    this.groupOrdersService.getGroupInvite$(reservationId).subscribe({
      next: (invite) => {
        this.invite.set(invite);

        forkJoin({
          sessions: this.groupOrdersService.getSessions$(invite.GroupReservationId),
          ownerSession: this.groupOrdersService.getOwnerSessionId$(reservationId),
          status: this.groupOrdersService.getStatus$(invite.GroupReservationId),
        }).subscribe({
          next: ({ sessions, ownerSession, status }) => {
            this.participants.set(sessions.participants);
            this.guestSessionId.set(ownerSession);
            this.status.set(status.status);
            this.isReadOnly.set(status.status !== 'Open');
            this.isLoading.set(false);
          },
          error: () => {
            this.error.set(true);
            this.isLoading.set(false);
          },
        });
      },
      error: () => {
        this.error.set(true);
        this.isLoading.set(false);
      },
    });
  }
  
  openSubmitModal(): void {
    if (this.hasSubmittedDishes() && !this.isReadOnly()) {
      this.isModalOpen.set(true);
    }
  }

  closeSubmitModal(): void {
    if (!this.isSubmitting()) {
      this.isModalOpen.set(false);
    }
  }

  submitPreOrder(): void {
    if (
      this.isSubmitting() ||
      this.isReadOnly() ||
      this.status() !== 'Open' ||
      !this.hasSubmittedDishes()
    ) {
      return;
    }

    const invite = this.invite();
    const reservationId = this.route.snapshot.paramMap.get('reservationId');

    if (!invite || !reservationId) {
      return;
    }

    this.isSubmitting.set(true);

    this.groupOrdersService
      .submit$(invite.GroupReservationId, reservationId)
      .subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.isModalOpen.set(false);
          this.status.set('Submitted');
          this.isReadOnly.set(true);

          this.toastService.success('Pre-order sent to the restaurant');
        },
        error: () => {
          this.isSubmitting.set(false);
        },
      });
  }
}