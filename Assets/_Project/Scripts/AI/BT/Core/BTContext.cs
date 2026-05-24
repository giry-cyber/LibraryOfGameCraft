using UnityEngine;

namespace LibraryOfGamecraft.BT
{
    // BT ノードが参照するすべてのランタイム情報を保持する。
    // ノードは BTContext 経由でコンポーネントや Blackboard にアクセスし、
    // Unity の具体的な API を直接知らなくていい。
    public class BTContext
    {
        public GameObject     Owner     { get; }
        public Transform      Transform => Owner.transform;
        public BTBlackboard   Blackboard { get; } = new BTBlackboard();

        public BTContext(GameObject owner)
        {
            Owner = owner;
        }

        // コンポーネントをキャッシュなしで取得する簡易アクセサ。
        // 頻繁に呼ぶなら能力コンポーネント側でキャッシュすること。
        public T Get<T>() where T : Component => Owner.GetComponent<T>();
    }
}
