export class TransactionOperationResult {
    constructor(result?: {});
    Index: any;
    OperationType: any;
    ObjectType: any;
    GUID: any;
    Success: any;
    Result: any;
    Error: any;
}
export default class TransactionResult {
    constructor(result?: {});
    Success: any;
    RolledBack: any;
    FailedOperationIndex: any;
    Error: any;
    Operations: any;
    DurationMs: any;
}
