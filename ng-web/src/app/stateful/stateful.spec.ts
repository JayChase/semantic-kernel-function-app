import {} from '@microsoft/signalr';
import {
  Observable,
  ReplaySubject,
  Subject,
  firstValueFrom,
  of,
  shareReplay,
  skip
} from 'rxjs';
import { Action, stateful } from './stateful';

type TestThing = {
  id: number;
  name: string;
};

function isSupersetOf<T>(set: Set<T>, subset: Set<T>) {
  for (const elem of subset) {
    if (!set.has(elem)) {
      return false;
    }
  }
  return true;
}

function difference<T>(setA: Set<T>, setB: Set<T>) {
  const _difference = new Set(setA);
  for (const elem of setB) {
    _difference.delete(elem);
  }
  return _difference;
}

describe('Stateful', () => {
  let testSource: Observable<TestThing[]>;
  let testFeed: Subject<Action<TestThing> | Action<TestThing>[]>;
  let test$: Observable<TestThing[]>;

  beforeEach(() => {
    testSource = of(
      [...new Array(5)].map((_, i) => ({
        id: i,
        name: i + ''
      }))
    ).pipe(shareReplay(1));

    testFeed = new ReplaySubject<Action<TestThing> | Action<TestThing>[]>(1);

    test$ = testSource.pipe(
      stateful(testFeed.asObservable(), (t1, t2) => t1.id === t2.id)
    );
  });

  it('should emit existing items when subscribed to', async () => {
    const sourceValues = await firstValueFrom(testSource);

    const existingValues = await firstValueFrom(test$);

    await expect(
      difference(new Set(existingValues), new Set(sourceValues)).size
    ).toBe(0);
  });

  it('should add a new item to the source output', async () => {
    const testItem = {
      id: 99,
      name: '99'
    };

    testFeed.next({
      change: 'add',
      item: testItem
    });

    await expect(
      (
        await firstValueFrom(test$.pipe(skip(1)))
      ).find((item) => item === testItem)
    ).toBeTruthy();
  });

  it('should add new items array to the source output', async () => {
    const testItems = new Set([
      {
        id: 99,
        name: '99'
      },
      {
        id: 100,
        name: '100'
      }
    ]);

    testFeed.next(
      [...testItems].map((item) => ({
        item,
        change: 'add'
      }))
    );

    const statefulItems = new Set(await firstValueFrom(test$.pipe(skip(1))));

    expect(isSupersetOf(statefulItems, testItems)).toBeTruthy();
  });

  it('should update an item', async () => {
    const testItems = new Set([
      {
        id: 100,
        name: '100!!'
      }
    ]);

    testFeed.next(
      [...testItems].map((item) => ({
        item,
        change: 'update'
      }))
    );

    const statefulItems = await firstValueFrom(test$.pipe(skip(1)));

    expect(statefulItems.find((item) => item.name === '100!!')).toBeTruthy();
  });

  it('should remove an item from the source output', async () => {
    const testItems = new Set([
      {
        id: 100,
        name: '100!!'
      }
    ]);

    testFeed.next(
      [...testItems].map((item) => ({
        item,
        change: 'update'
      }))
    );

    const currentItems = await firstValueFrom(test$);
    const itemToRemove = currentItems[0];

    testFeed.next({
      item: itemToRemove,
      change: 'remove'
    });

    const statefulItems = await firstValueFrom(test$.pipe(skip(1)));

    expect(statefulItems.find((item) => item === itemToRemove)).toBeFalsy();
  });
});
