using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class PreferredSlotChecks
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(object target,string field,object value)=>target.GetType().GetField(field,Private).SetValue(target,value);
    static Dictionary<EnemyHealthController,int> Map(TileManager manager,string field)=>(Dictionary<EnemyHealthController,int>)typeof(TileManager).GetField(field,Private).GetValue(manager);
    static int Slot(TileManager m,EnemyHealthController e)=>Map(m,"m_formationSlotByEnemy")[e];
    static int Reserved(TileManager m,EnemyHealthController e)=>Map(m,"m_arrivalSlotByEnemy")[e];
    static EnemyHealthController Spawn(EnemyObjectPool2D pool){var e=pool.Spawn(Vector3.zero,1,1);Check(e!=null,"Preferred-slot setup spawn rejected");Set(e.Movement,"m_moveSpeed",30f);return e;}
    static void Clear(TileManager m){foreach(var e in m.GetAllEnemiesOnPath())e.ReturnToPool();Check(Map(m,"m_arrivalSlotByEnemy").Count==0,"Reservation leak during cleanup");}
    static void Place(TileManager m,EnemyHealthController e,int index)
    {
        Check(m.SetEnemyAtPathIndex(e,index,out _),"Blocker setup failed");m.TryGetEnemyFormationPosition(e,out Vector3 position);e.transform.position=position;Set(e.Movement,"m_hasFormationTarget",false);
    }
    public static IEnumerator Verify(EnemyObjectPool2D pool,TileManager manager)
    {
        var actors=new EnemyHealthController[9];for(int i=0;i<9;i++){var e=Spawn(pool);actors[Slot(manager,e)]=e;}
        var destinations=new HashSet<Vector3>();
        for(int slot=8;slot>=0;slot--){Check(manager.TryReserveEnemyArrivalPosition(actors[slot],1,out Vector3 p)&&Reserved(manager,actors[slot])==slot,"Free destination must preserve each of the nine source slots even in reversed request order");Check(destinations.Add(p),"Two arrivals reserved the same slot");}
        Check(destinations.Count==9&&!manager.HasEnemyCapacityAtPathIndex(1),"Nine reservations must fill tile capacity");Clear(manager);
        Debug.Log("PASS all nine source positions preserved at destination, reversed reservation order, unique arrivals and nine-slot cap");

        var blocker=Spawn(pool);Check(Slot(manager,blocker)==4,"New spawn must start at center");Place(manager,blocker,1);
        var middle=Spawn(pool);var right=Spawn(pool);Check(Slot(manager,middle)==4&&Slot(manager,right)==5,"New spawn fallback must be rightward");
        Check(manager.TryReserveEnemyArrivalPosition(middle,1,out _)&&Reserved(manager,middle)==5,"Blocked center must choose immediate right");
        Check(manager.TryReserveEnemyArrivalPosition(right,1,out _)&&Reserved(manager,right)==6,"Occupied/reserved right edge must wrap to next row");
        blocker.ReturnToPool();Check(manager.TryReserveEnemyArrivalPosition(middle,1,out _)&&Reserved(manager,middle)==5,"Rechecking reservation must not pull enemy back into newly freed center");Clear(manager);

        for(int i=0;i<9;i++){var e=Spawn(pool);actors[Slot(manager,e)]=e;}Place(manager,actors[2],1);
        var edge=Spawn(pool);Check(Slot(manager,edge)==2,"Edge vacancy must be reused");Check(manager.TryReserveEnemyArrivalPosition(edge,1,out _)&&Reserved(manager,edge)==3,"Top-right blocked position must search next row instead of restarting at center");Clear(manager);
        Debug.Log("PASS center-to-right collision fallback, reserved slots counted, row-edge wrap and stable repeated reservations after vacancy");

        blocker=Spawn(pool);Place(manager,blocker,1);middle=Spawn(pool);
        Check(manager.TryReserveEnemyArrivalPosition(middle,1,out _)&&Reserved(manager,middle)==5,"Actual movement needs reserved right fallback");
        yield return middle.Movement.ProcessTurnMoveRoutine();Check(middle.Movement.CurrentTileIndex==1&&Slot(manager,middle)==5,"Actual collision fallback landing failed");
        yield return middle.Movement.ProcessTurnMoveRoutine();Check(middle.Movement.CurrentTileIndex==2&&Slot(manager,middle)==5,"Next empty tile reset shifted enemy to center");
        middle.Movement.ReverseNextDiceMove();yield return middle.Movement.ProcessTurnMoveRoutine();Check(middle.Movement.CurrentTileIndex==1&&Slot(manager,middle)==5,"Reverse corner move did not preserve source slot");Clear(manager);
        Debug.Log("PASS actual triple-roll collision detour, persistent relative slot on next tile, corners and reverse movement");

        for(int i=0;i<9;i++){var e=Spawn(pool);actors[Slot(manager,e)]=e;}
        for(int slot=8;slot>=0;slot--)actors[slot].Movement.StartCoroutine(actors[slot].Movement.ProcessTurnMoveRoutine());
        bool any=true;float deadline=Time.realtimeSinceStartup+5;
        while(any){Check(Time.realtimeSinceStartup<deadline,"Preferred-slot simultaneous moves stalled");any=false;foreach(var e in actors)any|=e.Movement.IsMoving;if(any)yield return null;}
        for(int slot=0;slot<9;slot++){
            Check(actors[slot].Movement.CurrentTileIndex==1&&Slot(manager,actors[slot])==slot,"Concurrent traversal did not preserve original position");
            manager.TryGetEnemyFormationPosition(actors[slot],out Vector3 p);Check(Vector3.Distance(p,actors[slot].transform.position)<.001f,"Actor did not land on reserved original position");
        }
        Clear(manager);var reused=Spawn(pool);Check(Slot(manager,reused)==4,"Pool reuse retained a stale preferred slot instead of initial center");reused.ReturnToPool();
        Debug.Log("PASS all nine actual concurrent triple-roll moves preserve slots, exact landings and no preferred-slot memory leaks after pooling");
    }
}
