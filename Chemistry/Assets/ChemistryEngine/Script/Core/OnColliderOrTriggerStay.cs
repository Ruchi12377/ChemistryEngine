using System;
using UnityEngine;

public class OnColliderOrTriggerStay : MonoBehaviour
{
    public Observable<GameObject> OnStayObservable { get; private set; }

    public void Init(Action<GameObject> subscribe)
    {
        OnStayObservable = new Observable<GameObject>(_ =>
        {
            subscribe.Invoke(gameObject);
        });
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
        new Observable<GameObject>(x =>
        {
            OnStayObservable.Subscribe(new Observer<GameObject>(y =>
            {
                x.OnNext(go);
            }));
        }).Subscribe(new Observer<GameObject>(g => { }));
    }
}

public static class OnColliderOrTriggerStayEx
{
    public static Observable<GameObject> OnColliderOrTriggerStayAsObservable(this MonoBehaviour mono, Action<GameObject> subscribe)
    {
        var component = mono.gameObject.AddComponent<OnColliderOrTriggerStay>();
        var onStay = component.OnStayObservable;
        component.Init(subscribe);
        return onStay;
    }
}


public class Observer<T>
{
    private readonly Action<T> _onNext;

    public Observer(Action<T> onNext) => _onNext = onNext;
    public void OnNext(T v) => _onNext.Invoke(v);
}

public class Observable<T>
{
    private readonly Action<Observer<T>> _subscribe;

    public Observable(Action<Observer<T>> subscribe) => _subscribe = subscribe;
    public void Subscribe(Observer<T> observer) => _subscribe(observer);
}