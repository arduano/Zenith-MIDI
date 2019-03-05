import {
    trigger,
    animate,
    transition,
    style,
    query,
    group
} from '@angular/animations';
import { delay } from 'rxjs/operators';

export const routerTransition = trigger('routerTransition', [
    transition('* => *', [
        query(':enter, :leave', style({ position: 'fixed', width:'100%' })
        , { optional: true }),
        query(
          ':enter',
          [style({ opacity: 0 })],
          { optional: true }
        ),
        query(':leave', [
          style({ transform: 'translateX(0%)' }),
          animate('0.2s cubic-bezier(.7,-0.01,1,1.01)', style({ transform: 'translateY(5vh)', opacity: 0 }))
        ], { optional: true }),
        query(':enter', [
          style({ transform: 'translateY(5vh)', opacity: 0 }),
          animate('0.4s cubic-bezier(.17,.67,.23,.99)', style({ transform: 'translateY(0vh)', opacity: 1 }))
        ], { optional: true }),
    ])
]);

export const formTransition = trigger('formTransition', [
  transition('* => *', [
      query('app-form-success, app-general-form', style({ position: 'fixed', width:'100%' })
        , { optional: true }),
        query(
          'app-form-success',
          [style({ opacity: 0 })],
          { optional: true }
        ),
        query('app-general-form', [
          style({ transform: 'translateX(0%)' }),
          animate('0.2s cubic-bezier(.7,-0.01,1,1.01)', style({ transform: 'translateY(2vh)', opacity: 0 }))
        ], { optional: true }),
        query('app-form-success', [
          style({ transform: 'translateY(-2vh)', opacity: 0 }),
          animate('0.4s cubic-bezier(.17,.67,.23,.99)', style({ transform: 'translateY(0vh)', opacity: 1 }))
        ], { optional: true }),
    ])
]);

export const worksTransition = trigger('worksTransition', [
  transition('* => *', [
      query(':enter, :leave', style({ position: 'fixed', width:'100%' })
        , { optional: true }),
        query(
          ':enter',
          [style({ opacity: 0 })],
          { optional: true }
        ),
        query(':leave', [
          style({ transform: 'translateX(0%)' }),
          animate('0.2s cubic-bezier(.7,-0.01,1,1.01)', style({ transform: 'translateY(2vh)', opacity: 0 }))
        ], { optional: true }),
        query(':enter', [
          style({ transform: 'translateY(-2vh)', opacity: 0 }),
          animate('0.4s cubic-bezier(.17,.67,.23,.99)', style({ transform: 'translateY(0vh)', opacity: 1 }))
        ], { optional: true }),
    ])
]);