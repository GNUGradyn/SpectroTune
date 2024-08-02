namespace SpectroTune;

public static class Extensions
{
    public static int IndexOf<T>(this IReadOnlyList<T> list, T item){
        var e = list.GetEnumerator();
        var i = 0;

        while(e.MoveNext()){
            if(ReferenceEquals(e.Current, item)){
                return i;
            }
            i++;
        }
        return -1;
    }
}