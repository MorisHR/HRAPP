export interface LegalHold {
  id: string;
  tenantId?: string;
  caseNumber: string;
  description: string;
  reason?: string;
  startDate: Date;
  endDate?: Date;
  status: LegalHoldStatus;

  userIds?: string;
  entityTypes?: string;
  searchKeywords?: string;

  requestedBy: string;
  legalRepresentative?: string;
  legalRepresentativeEmail?: string;
  lawFirm?: string;
  courtOrder?: string;

  releasedBy?: string;
  releasedAt?: Date;
  releaseNotes?: string;

  affectedAuditLogCount: number;
  affectedEntityCount: number;

  notifiedUsers?: string;
  notificationSentAt?: Date;

  complianceFrameworks?: string;
  retentionPeriodDays?: number;

  createdAt: Date;
  createdBy: string;
  updatedAt?: Date;
  updatedBy?: string;
  additionalMetadata?: string;
}

export enum LegalHoldStatus {
  PENDING = 'PENDING',
  ACTIVE = 'ACTIVE',
  RELEASED = 'RELEASED',
  EXPIRED = 'EXPIRED',
  CANCELLED = 'CANCELLED'
}

export interface EDiscoveryPackage {
  legalHoldId: string;
  caseNumber: string;
  generatedAt: Date;
  recordCount: number;
  format: EDiscoveryFormat;
  fileUrl: string;
  chainOfCustody: string;
}

export enum EDiscoveryFormat {
  EMLX = 'EMLX',
  PDF = 'PDF',
  JSON = 'JSON',
  CSV = 'CSV',
  NATIVE = 'NATIVE'
}
