import {
  Observable,
  combineLatest,
  map,
  scan,
  shareReplay,
  startWith,
  switchMap
} from 'rxjs';

export type Action<T> = {
  item: T;
  change: 'add' | 'remove' | 'update';
};

export type CompareWith<T> = (item1: T, item2: T) => boolean;

export type StateManager<T, FT> = (state: T[], feed: FT | null) => T[];

/**
 * Keep the source observable up to date with changes from the changeFeed
 * @param changeFeed
 * @param compareWith
 * @returns Observable<T>
 */
export function stateful<T, FT>(
  changeFeed: Observable<FT | null>,
  stateManager: StateManager<T, FT>
) {
  return (source: Observable<T[]>) => {
    return source.pipe(
      switchMap(() =>
        combineLatest([source, changeFeed.pipe(startWith(null))]).pipe(
          scan(([state], [, feed]) => {
            const updatedState = stateManager(state, feed);

            const newState: [T[], FT | null] = [updatedState, feed];

            return newState;
          }),
          map(([state]) => state),
          shareReplay({
            refCount: true,
            bufferSize: 1
          })
        )
      )
    );
  };
}

/**
 * Keep the source observable up to date with changes from the changeFeed
 * @param changeFeed
 * @returns Observable<T>
 */
export function statefulSingleton<T, FT>(changeFeed: Observable<FT | null>) {
  return (source: Observable<T>) => {
    return source.pipe(
      switchMap(() =>
        combineLatest([source, changeFeed.pipe(startWith(null))]).pipe(
          scan((state, feed) => {
            return feed || state;
          }),
          map(([state]) => state),
          shareReplay({
            refCount: true,
            bufferSize: 1
          })
        )
      )
    );
  };
}
