using UnityEngine;

public class OnColliderOrTriggerStay : MonoBehaviour
{
    private Observer<GameObject> _source;
    
    public Observable<GameObject> Init()
    {
        return new Observable<GameObject>(o => _source = o);
    }
    
    private void OnCollisionStay(Collision other)
    {
        OnStay(other.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        OnStay(other.gameObject);
    }

    private void OnStay(GameObject go)
    {
        _source.OnNext(go);
    }
}

public static class OnColliderOrTriggerStayEx
{
    public static Observable<GameObject> OnColliderOrTriggerStayAsObservable(this MonoBehaviour mono)
    {
        var component = mono.gameObject.AddComponent<OnColliderOrTriggerStay>();
        return component.Init();
    }
    
    public static void Subscribe<T>(this Observable<T> observable, System.Action<T> subscribe) =>
        observable.Subscribe(new Observer<T>(v => subscribe(v)));

    public static Observable<U> Select<T, U>(this Observable<T> observable, System.Func<T, U> select)
    {
        return new Observable<U>(o =>
        {
            observable.Subscribe(v =>
            {
                o.OnNext(select(v));
            });
        });
    }

    public static Observable<T> Where<T>(this Observable<T> observable, System.Func<T, bool> where)
    {
        return new Observable<T>(o =>
        {
            observable.Subscribe(v =>
            {
                if (where(v))
                {
                    o.OnNext(v);
                }
            });
        });
    }
}


public class Observer<T>
{
    private readonly System.Action<T> _onNext;

    public Observer(System.Action<T> onNext) => this._onNext = onNext;
    public void OnNext(T v) => _onNext.Invoke(v);
}

public class Observable<T>
{
    private readonly System.Action<Observer<T>> _subscribe;

    public Observable(System.Action<Observer<T>> subscribe) => _subscribe = subscribe;
    public void Subscribe(Observer<T> observer) => _subscribe(observer);
}