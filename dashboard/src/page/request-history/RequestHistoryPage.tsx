'use client';
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, Input, Select, Space, Table, Tag, Tooltip, Typography, Popconfirm } from 'antd';
import { DeleteOutlined, ReloadOutlined, EyeOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import toast from 'react-hot-toast';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import { globalToastId } from '@/constants/config';
import RequestHistoryChart from './RequestHistoryChart';
import RequestHistoryDetailModal from './RequestHistoryDetailModal';
import {
  deleteRequestHistory,
  listRequestHistory,
  RequestHistoryEntry,
  RequestHistorySearchResult,
} from '@/lib/sdk/requestHistory';

const { Text } = Typography;

const STATUS_OPTIONS = [
  { value: '', label: 'All statuses' },
  { value: '200', label: '200' },
  { value: '201', label: '201' },
  { value: '204', label: '204' },
  { value: '400', label: '400' },
  { value: '401', label: '401' },
  { value: '403', label: '403' },
  { value: '404', label: '404' },
  { value: '500', label: '500' },
];

const METHOD_OPTIONS = [
  { value: '', label: 'All methods' },
  'GET',
  'POST',
  'PUT',
  'DELETE',
  'HEAD',
  'PATCH',
].map((m) => (typeof m === 'string' ? { value: m, label: m } : m));

const statusColor = (code: number): string => {
  if (code >= 200 && code < 300) return 'green';
  if (code >= 300 && code < 400) return 'blue';
  if (code >= 400 && code < 500) return 'gold';
  return 'red';
};

const methodColor = (method: string): string => {
  switch (method.toUpperCase()) {
    case 'GET':
      return 'green';
    case 'POST':
      return 'blue';
    case 'PUT':
    case 'PATCH':
      return 'gold';
    case 'DELETE':
      return 'red';
    default:
      return 'default';
  }
};

type Props = {
  tenantScope?: string;
  mode: 'admin' | 'tenant';
};

const RequestHistoryPage: React.FC<Props> = ({ tenantScope, mode }) => {
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(25);
  const [method, setMethod] = useState<string>('');
  const [statusCode, setStatusCode] = useState<string>('');
  const [path, setPath] = useState<string>('');
  const [result, setResult] = useState<RequestHistorySearchResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const [selected, setSelected] = useState<RequestHistoryEntry | null>(null);
  const [detailOpen, setDetailOpen] = useState(false);

  const fetchList = useCallback(() => {
    setLoading(true);
    listRequestHistory({
      page,
      pageSize,
      method: method || undefined,
      statusCode: statusCode ? Number(statusCode) : undefined,
      path: path || undefined,
      tenantGuid: mode === 'tenant' ? tenantScope : undefined,
    })
      .then((data) => setResult(data))
      .catch(() => {
        setResult(null);
        toast.error('Unable to load request history', { id: globalToastId });
      })
      .finally(() => setLoading(false));
  }, [page, pageSize, method, statusCode, path, mode, tenantScope, refreshKey]);

  useEffect(() => {
    fetchList();
  }, [fetchList]);

  const onDelete = async (id: string) => {
    try {
      await deleteRequestHistory(id);
      toast.success('Entry deleted', { id: globalToastId });
      setRefreshKey((n) => n + 1);
    } catch {
      toast.error('Unable to delete', { id: globalToastId });
    }
  };

  const onView = (entry: RequestHistoryEntry) => {
    setSelected(entry);
    setDetailOpen(true);
  };

  const columns: ColumnsType<RequestHistoryEntry> = useMemo(
    () => [
      {
        title: 'Time',
        dataIndex: 'CreatedUtc',
        width: 170,
        render: (v: string) => new Date(v).toLocaleString(),
      },
      {
        title: 'Method',
        dataIndex: 'Method',
        width: 80,
        render: (v: string) => <Tag color={methodColor(v)}>{v}</Tag>,
      },
      {
        title: 'Path',
        dataIndex: 'Path',
        ellipsis: true,
        render: (v: string) => <Text code>{v}</Text>,
      },
      {
        title: 'Status',
        dataIndex: 'StatusCode',
        width: 80,
        render: (v: number) => <Tag color={statusColor(v)}>{v}</Tag>,
      },
      {
        title: 'Duration',
        dataIndex: 'ProcessingTimeMs',
        width: 100,
        render: (v: number) => `${v.toFixed(1)} ms`,
      },
      {
        title: 'Size (req/resp)',
        width: 140,
        render: (_: unknown, r: RequestHistoryEntry) =>
          `${r.RequestBodyLength} / ${r.ResponseBodyLength} B`,
      },
      ...(mode === 'admin'
        ? [
            {
              title: 'Tenant',
              dataIndex: 'TenantGUID',
              width: 160,
              render: (v?: string | null) =>
                v ? (
                  <Text code style={{ fontSize: 11 }}>
                    {v.substring(0, 8)}
                  </Text>
                ) : (
                  '-'
                ),
            },
          ]
        : []),
      {
        title: 'Actions',
        width: 120,
        fixed: 'right' as const,
        render: (_: unknown, r: RequestHistoryEntry) => (
          <Space size="small">
            <Tooltip title="View detail">
              <Button
                size="small"
                type="text"
                icon={<EyeOutlined />}
                onClick={() => onView(r)}
              />
            </Tooltip>
            <Popconfirm
              title="Delete this entry?"
              onConfirm={() => onDelete(r.GUID)}
              okText="Delete"
              cancelText="Cancel"
            >
              <Button size="small" type="text" danger icon={<DeleteOutlined />} />
            </Popconfirm>
          </Space>
        ),
      },
    ],
    [mode]
  );

  return (
    <PageContainer
      id="request-history"
      pageTitle={
        <Space>
          <span>Request History</span>
          <Tooltip title="Refresh">
            <Button
              type="text"
              icon={<ReloadOutlined spin={loading} />}
              onClick={() => setRefreshKey((n) => n + 1)}
            />
          </Tooltip>
        </Space>
      }
    >
      <RequestHistoryChart
        tenantGuid={mode === 'tenant' ? tenantScope : undefined}
        refreshKey={refreshKey}
      />

      <Space style={{ marginBottom: 12, flexWrap: 'wrap' }}>
        <Select
          style={{ width: 140 }}
          value={method}
          onChange={(v) => {
            setMethod(v);
            setPage(0);
          }}
          options={METHOD_OPTIONS}
        />
        <Select
          style={{ width: 140 }}
          value={statusCode}
          onChange={(v) => {
            setStatusCode(v);
            setPage(0);
          }}
          options={STATUS_OPTIONS}
        />
        <Input
          placeholder="Path contains..."
          value={path}
          onChange={(e) => setPath(e.target.value)}
          onPressEnter={() => setPage(0)}
          style={{ width: 280 }}
          allowClear
        />
      </Space>

      <Table<RequestHistoryEntry>
        loading={loading}
        columns={columns}
        dataSource={result?.Objects || []}
        rowKey="GUID"
        size="small"
        scroll={{ x: 1000 }}
        onRow={(record) => ({
          onClick: (e) => {
            const target = e.target as HTMLElement;
            if (target.closest('button') || target.closest('.ant-popover')) return;
            onView(record);
          },
          style: { cursor: 'pointer' },
        })}
        pagination={{
          current: page + 1,
          pageSize,
          total: result?.TotalCount || 0,
          showSizeChanger: true,
          pageSizeOptions: ['10', '25', '50', '100'],
          onChange: (p, ps) => {
            setPage(p - 1);
            setPageSize(ps);
          },
        }}
      />

      <RequestHistoryDetailModal
        entry={selected}
        open={detailOpen}
        onClose={() => setDetailOpen(false)}
      />
    </PageContainer>
  );
};

export default RequestHistoryPage;
