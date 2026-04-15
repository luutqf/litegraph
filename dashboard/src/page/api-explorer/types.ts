export type ApiParameter = {
  name: string;
  in: 'path' | 'query' | 'header' | 'cookie';
  required?: boolean;
  description?: string;
  schema?: unknown;
};

export type ApiOperation = {
  id: string;
  method: string;
  path: string;
  summary?: string;
  description?: string;
  tag: string;
  parameters: ApiParameter[];
  requestBodyExample?: string;
  hasRequestBody: boolean;
};

export type RecentRequest = {
  id: string;
  method: string;
  path: string;
  url: string;
  timestamp: string;
  status?: number;
};

export type ApiResponseState = {
  status: number;
  statusText: string;
  headers: Record<string, string>;
  contentType: string;
  text: string;
  json: unknown;
  durationMs: number;
  url: string;
};
