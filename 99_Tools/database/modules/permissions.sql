-- Execute-only roles rely on static SQL and the common dbo ownership chain.
GRANT EXECUTE ON OBJECT::dh.ReadAdmission TO dh_runtime;
GRANT EXECUTE ON OBJECT::dh.AcquireAndLoad TO dh_runtime;
GRANT EXECUTE ON OBJECT::dh.WriteSafeCheckpoint TO dh_runtime;
GRANT EXECUTE ON OBJECT::dh.ReleaseRuntime TO dh_runtime;
GRANT EXECUTE ON OBJECT::dh.ResolveRuntimeOperation TO dh_runtime;
GRANT EXECUTE ON OBJECT::dh.InspectRecovery TO dh_recovery;
GRANT EXECUTE ON OBJECT::dh.RecoverAndLoad TO dh_recovery;
GRANT EXECUTE ON OBJECT::dh.ReleaseRecovery TO dh_recovery;
GRANT EXECUTE ON OBJECT::dh.ResolveRecoveryOperation TO dh_recovery;
