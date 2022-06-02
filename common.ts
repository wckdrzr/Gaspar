//**
//** This file was written by a tool
//**
//** It contains all models and enums in:
//**     /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/**/*.cs
//**     only if they are attributed: [FrontEnd]
//**
//** full configuration in: /Users/ben/WCKDRZR/csharp-walker/csharp-models-to-typescript.config.json
//**


//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Abstract/TaggedPhysical.cs


export interface TaggedPhysical {
    tags: CatalogueTag[];
    uniqueTag: string | null;
    externalId: ExternalId[];
    dataOwnerUsername: string | null;
    hasDataOwnerSet: boolean;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Objects/LogLevel.cs


export enum LogLevel {
    Verbose = 0,
    Debug = 1,
    Info = 2,
    Warn = 3,
    Error = 4,
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Objects/DatabaseTypes.cs


export enum DatabaseType {
    Unknown = 0,
    AmazonAurora = 1,
    AmazonRDS = 2,
    MariaDB = 3,
    MongoDB = 4,
    MySQL = 5,
    Postgres = 6,
    Oracle = 7,
    MSSQL = 8,
    Sybase = 9,
    GoogleBigQuery = 10,
    GoogleBigTable = 11,
    IBMDB2 = 12,
    ApacheHive = 13,
    ApacheHBase = 14,
}

export interface DatabaseType_Properties {
    id: number;
    name: string;
    defaultPort: string | null;
    displayName: string | null;
    hideServerAndPort: boolean;
    connectionOptions: DatabaseOption[];
    manualCredentialOptions: DatabaseOption[];
}

export interface DatabaseOption {
    name: string | null;
    fieldType: string | null;
    listOptions: string[];
    interfaceWidth?: number;
    value: string | null;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Objects/PhysicalField.cs


export interface PhysicalField {
    schema: string | null;
    table: string | null;
    column: string | null;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Objects/Decision.cs


export interface Decision extends Object {
    redactionType: RedactionType;
    auditAction: AuditAction;
    schema: string | null;
    table: string | null;
    column: string | null;
    tags: string[];
    ruleTypeOrder: number;
    conditions: IRuleCondition;
    ruleIds: string[];
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Objects/SystemTag.cs


export enum SystemTag {
    None = 0,
    address = 1,
    city = 2,
    firstName = 3,
    fullName = 4,
    lastName = 5,
    country = 6,
    businessName = 7,
    id = 8,
    phone = 9,
    postcode = 10,
    state = 11,
    email = 12,
    date = 13,
    gender = 14,
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Objects/DNARestResult.cs


export interface DNARestResult {
    columnDefinition: Record<string, string>;
    rows: Record<string, any>[];
    databaseName: string | null;
    tableName: string | null;
    hasChanged: boolean;
    errors: string[];
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Objects/RedactionType.cs


export enum RedactionType {
    Error_None_Set = 0,
    Permit = 1,
    Hash = 2,
    Redact = 3,
    Remove = 4,
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Objects/RestrictionType.cs


export enum RestrictionType {
    Full = 0,
    Partial = 1,
    None = 2,
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Objects/DecisionSet.cs


export interface DecisionSet {
    decisionsSet: Record<string, Record<string, Record<string, Decision>>>;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Objects/PolicyDecision.cs


export interface PolicyDecision {
    dataRestrictionType: RestrictionType;
    decisionSet: DecisionSet;
    id: string | null;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Objects/DatabaseState.cs


export enum DatabaseState {
    Discovered = 0,
    Configured = 1,
    Spidered = 2,
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Objects/AuditAction.cs


export enum AuditAction {
    None = 0,
    Log = 1,
    Flag = 2,
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Objects/Status.cs


export enum Status {
    Inactive = 0,
    Active = 1,
    Deleted = 2,
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Objects/DiscoveryEntry.cs


export interface DiscoveryEntry {
    databaseType: DatabaseType;
    serverAddress: string | null;
    serverPort: string | null;
    optionalConnectionDetail: Record<string, string>;
    userName: string | null;
    password: string | null;
    persistCredentials: boolean;
    credentialsMode: number;
    useServiceAccount: boolean;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Objects/SpiderRequest.cs


export interface SpiderRequest {
    databaseType: DatabaseType;
    serverAddress: string | null;
    serverPort: string | null;
    optionalConnectionDetail: Record<string, string>;
    optionalManualCredentialDetail: Record<string, string>;
    userName: string | null;
    password: string | null;
    persistCredentials: boolean;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Objects/IntegrationPartner.cs


export enum IntegrationPartner {
    COLLIBRA = 0,
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/Notification.cs


export interface Notification {
    notificationId: string | null;
    userId: string | null;
    unread: boolean;
    message: string | null;
    onClickLink: string | null;
    createdOn: string;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/RuleConditions.cs


export interface IRuleCondition {
    conditionType: ConditionType;
}

export interface IActionableCondition extends Object, Object, IRuleCondition {
    redactionType: RedactionType;
}

export interface NoneCondition extends IRuleCondition {
    conditionType: ConditionType;
}

export interface MultiCondition extends IRuleCondition {
    conditionType: ConditionType;
    conditions: IActionableCondition[];
}

export interface GroupCondition extends IRuleCondition, IActionableCondition, Object {
    conditionType: ConditionType;
    gate: ConditionGate;
    conditions: IActionableCondition[];
    redactionType: RedactionType;
}

export interface SingleCondition extends IRuleCondition, IActionableCondition {
    conditionType: ConditionType;
    tag: string | null;
    taggedField: string | null;
    condition: ISingleCondition;
    redactionType: RedactionType;
}

export enum ConditionType {
    None = 0,
    Single = 1,
    Group = 2,
    Multi = 3,
}

export enum ConditionGate {
    ERROR_NONE_SET = 0,
    AND = 1,
    OR = 2,
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/CatalogueTag.cs


export interface CatalogueTag {
    name: string | null;
    confidence: number;
    spiderTag: boolean;
    removed: boolean;
}

export interface TagInCatalogue extends CatalogueTag {
    locations: TagLocation[];
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/LocationTags.cs


export interface LocationTags {
    location: TagLocation;
    tags: CatalogueTag[];
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/DataRequest.cs


export enum RequestStatus {
    Pending = 0,
    InProgress = 1,
    Approved = 2,
    Denied = 3,
    Cancelled = 4,
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/LoggedPolicyDecision.cs


export interface LoggedPolicyDecision {
    id: string | null;
    generatedAt: string;
    user: PublicUser;
    schema: Schema;
    matchedRules: Rule[];
    policyDecision: PolicyDecision;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/DisplayPolicyDecision.cs


export interface DisplayPolicyDecision {
    loggedQuery: LoggedQuery;
    loggedPolicyDecision: LoggedPolicyDecision;
    filteredFlatDecisions: Decision[];
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/Field.cs


export interface Field extends TaggedPhysical {
    fieldName: string | null;
    logicalName: string | null;
    visibility: number;
    dataTypeName: string | null;
    dataTypeLength?: number;
    dataTypeUnsigned?: boolean;
    isKey: boolean;
    autotaggingComplete: boolean;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/DashboardItem.cs


export interface DashboardItem {
    x: number;
    y: number;
    backendHandle: string | null;
    component: string | null;
    cols: number;
    rows: number;
    minItemCols: number;
    minItemRows: number;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/LogicalNameLocation.cs


export interface LogicalNameLocation {
    server: string | null;
    port: string | null;
    schema: string | null;
    table: string | null;
    field: string | null;
    logicalName: string | null;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/User.cs


export interface PublicUser {
    id: string | null;
    userPredicates: Record<string, string>;
    userGroupPredicates: string[];
    userID: string | null;
    firstName: string | null;
    lastName: string | null;
    emailAddress: string | null;
    lineManager: LineManager;
}

export interface LineManager {
    id: string | null;
    firstName: string | null;
    lastName: string | null;
    email: string | null;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/DashboardResponse.cs


export interface DashboardResponse {
    catalogue: CatalogueMetric;
    rules: RulesMetric;
    tags: TagsMetric;
    reportExecutions: ReportMetric[];
    queryFrequency: WeekQueryPeriod[];
    historyDecisions: QueryWithDecisionMetric[];
}

export interface CatalogueMetric {
    servers: number;
    schemas: number;
    tables: number;
    fields: number;
}

export interface RulesMetric {
    rules: number;
    rulesHistory: number;
}

export interface TagsMetric {
    systemTags: number;
    manualTags: number;
    tagsUsed: number;
    tagsUnused: number;
    taggedEntries: number;
    untaggedEntries: number;
}

export interface ReportMetric {
    reportId: string | null;
    reportName: string | null;
    count: number;
}

export interface WeekQueryPeriod {
    name: string;
    series: DayQueryPeriod[];
}

export interface DayQueryPeriod {
    date: string;
    name: string | null;
    value: number;
}

export interface QueryWithDecisionMetric {
    restrictedCalls: number;
    partiallyRestrictedCalls: number;
    grantedCalls: number;
    date: string;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/ExternalId.cs


export interface ExternalId {
    id: string | null;
    url: string | null;
    isPush: boolean;
    partner: IntegrationPartner;
    probability: number;
    isProvisional: boolean;
    assetName: string | null;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/Audit.cs


export interface Audit {
    id: string | null;
    timestamp: string;
    userId: string | null;
    originalQuery: string | null;
    modifiedQuery: string | null;
    decisionId: string | null;
    modifiedFields: ModifiedField[];
    actionedFields: ActionedField[];
    hasBeenActioned: boolean;
}

export interface ModifiedField {
    field: PhysicalField;
    redactionType: RedactionType;
}

export interface ActionedField {
    field: PhysicalField;
    actions: AuditAction[];
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/LoggedQuery.cs


export interface LoggedQuery {
    id: string | null;
    timestamp: string;
    username: string | null;
    query: string | null;
    requestedTables: Record<string, string[]>;
    policyDecisionId: string | null;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/Log.cs


export interface Log {
    id: string | null;
    timestamp: string;
    logLevel: LogLevel;
    service: string | null;
    userID: string | null;
    message: string | null;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/Table.cs


export interface Table extends TaggedPhysical {
    tableName: string | null;
    schemaPrefix: string | null;
    logicalName: string | null;
    visibility: number;
    rows: number;
    sizeBytes: number;
    fields: Field[];
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/Report.cs


export interface Report {
    id: string | null;
    name: string | null;
    icon: string | null;
    sql: string | null;
    createdBy: string | null;
    created: string;
    modified: string;
    favourite: Boolean;
    recent: Boolean;
}

export interface ReportData {
    report: Report;
    result: DNARestResult;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/TagLocation.cs


export interface TagLocation {
    server: string | null;
    port: string | null;
    schema: string | null;
    table: string | null;
    field: string | null;
    autoTag: boolean;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/Catalogue.cs


export interface Catalogue extends TaggedPhysical {
    id: string | null;
    databaseType: DatabaseType;
    status: Status;
    state: DatabaseState;
    serverAddress: string | null;
    serverPort: string | null;
    optionalConnectionDetail: Record<string, string>;
    logicalName: string | null;
    username: string | null;
    usernameSalt: string;
    password: string | null;
    passwordSalt: string;
    schemas: Schema[];
    lastCrawled: string;
    location: Location;
    autotaggingDate: string;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/Schema.cs


export interface Schema extends TaggedPhysical {
    schemaName: string | null;
    logicalName: string | null;
    visibility: number;
    tables: Table[];
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/RuleTemplate.cs


export interface RuleTemplate {
    id: string | null;
    name: string | null;
    description: string | null;
    ruleIds: string[];
    global: boolean;
    filters: UserFilters;
}

export interface UserFilters {
    gate: Gate;
    filters: IUserFilter[];
}

export interface IUserFilter {
    filterType: string | null;
}

export interface UserGroupPredicateFilter extends IUserFilter {
    filterType: string | null;
    groupName: string | null;
}

export interface UserAttributePredicateFilter extends IUserFilter {
    filterType: string | null;
    attributeKey: string | null;
    attributeValue: string | null;
}

export enum Gate {
    INVALID_NONE_SET = 0,
    ALL = 1,
    ANY = 2,
    NONE = 3,
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/Location.cs


export interface Location {
    id: string | null;
    locationName: string | null;
    serverAddressMatch: string | null;
    geoPoint: any;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/Dashboard.cs


export interface Dashboard {
    id: string | null;
    name: string | null;
    items: DashboardItem[];
    isActive: boolean;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/RuleType.cs


export interface RuleType {
    id: string | null;
    ruleTypeName: string | null;
    order: number;
    enforce: boolean;
    audit: boolean;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/Rule.cs


export interface Rule extends Object {
    id: string | null;
    ruleTypeId: string | null;
    redactionType: RedactionType;
    auditAction: AuditAction;
    tags: string[];
    conditions: IRuleCondition;
    active: boolean;
    isExceptional: boolean;
    inverse: boolean;
    isLinked: boolean;
    linkId: string | null;
    hasNotBefore: boolean;
    notBefore: string;
    hasNotAfter: boolean;
    notAfter: string;
    ruleVersion: number;
    createdOn: string;
    templates: RuleTemplate[];
}

export interface LinkedRule {
    primaryRule: Rule;
    secondaryRule: Rule;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/TagType.cs


export interface TagType {
    id: string | null;
    name: string | null;
    emoji: string | null;
    base64Icon: string | null;
    entries: TagTypeEntry[];
}

export interface TagTypeEntry {
    id: string | null;
    name: string | null;
    emoji: string | null;
    base64Icon: string | null;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Models/Ontology.cs


export interface OntologyEntry {
    id: string | null;
    name: string | null;
    searchableValues: string[];
    systemTag: SystemTag;
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Extensions/SpiderRequestExtensions.cs


export interface SpiderRequestExtensions {
}

//File: /Users/ben/WCKDRZR/data-watchdog/libraries/Common/src/Common/Interfaces/StringCondition.cs


export interface ISingleCondition {
    singleConditionType: SingleConditionType;
}

export interface SingleStringCondition extends ISingleCondition, Object {
    singleConditionType: SingleConditionType;
    comparator: SingleStringConditionComparator;
    targetValue: string | null;
}

export interface SingleNumericCondition extends ISingleCondition, Object {
    singleConditionType: SingleConditionType;
    comparator: SingleNumericConditionComparator;
    targetValue: number;
}

export enum SingleConditionType {
    SINGLE_STRING = 0,
    MULTI_STRING = 1,
    NUMERIC = 2,
}

export enum SingleStringConditionComparator {
    ERROR_NONE_SET = 0,
    IS = 1,
    IS_NOT = 2,
    CONTAINS = 3,
}

export enum SingleNumericConditionComparator {
    ERROR_NONE_SET = 0,
    IS = 1,
    IS_NOT = 2,
    IS_GREATER_THAN = 3,
    IS_GREATER_THAN_OR_EQUAL_TO = 4,
    IS_LESS_THAN = 5,
    IS_LESS_THAN_OR_EQUAL_TO = 6,
    IS_BETWEEN = 7,
}
