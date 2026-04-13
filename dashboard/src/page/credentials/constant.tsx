import React from 'react';
import { Button, Dropdown, TableProps } from 'antd';
import {
  MoreOutlined,
  CheckCircleFilled,
  CloseCircleFilled,
  CodeOutlined,
} from '@ant-design/icons';
import CopyButton from '@/components/base/copy-button/CopyButton';
import { CredentialType } from '@/types/types';
import { formatDateTime } from '@/utils/dateUtils';
import { FilterDropdownProps } from 'antd/es/table/interface';
import TableSearch from '@/components/table-search/TableSearch';
import { onGUIDFilter, onNameFilter } from '@/constants/table';
import { columnTooltip } from '@/utils/tooltipUtils';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';

const monoCellStyle = {
  display: 'inline-flex',
  alignItems: 'center',
  gap: 4,
  fontFamily: 'monospace',
  fontSize: 12,
  maxWidth: '100%',
  minWidth: 0,
  width: '100%',
} as const;

const monoValueStyle = {
  display: 'block',
  flex: '1 1 auto',
  minWidth: 0,
  overflow: 'hidden',
  textOverflow: 'ellipsis',
  whiteSpace: 'nowrap',
} as const;

export const tableColumns = (
  handleEdit: (user: CredentialType) => void,
  handleDelete: (user: CredentialType) => void,
  handleViewJson?: (record: CredentialType) => void
): TableProps<CredentialType>['columns'] => [
  {
    title: columnTooltip('GUID', 'Globally unique identifier'),
    dataIndex: 'GUID',
    key: 'GUID',
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search GUID" />
    ),
    onFilter: (value, record) => onGUIDFilter(value, record.GUID),
    width: 220,
    ellipsis: true,
    render: (GUID: string) => (
      <span style={monoCellStyle} title={GUID}>
        <span style={monoValueStyle}>{GUID}</span>
        <CopyButton text={GUID} tooltipTitle="Copy GUID" />
      </span>
    ),
  },
  {
    title: columnTooltip('User', 'Associated user name'),
    dataIndex: 'userName',
    key: 'userName',
    width: 150,
    ellipsis: true,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search User" />
    ),
    onFilter: (value, record) => onNameFilter(value, record.userName || ''),
    render: (userName: string) => <div>{userName}</div>,
  },
  {
    title: columnTooltip('Name', 'Credential display name'),
    dataIndex: 'Name',
    key: 'name',
    width: 150,
    ellipsis: true,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Name" />
    ),
    onFilter: (value, record) => onNameFilter(value, record.Name),
    sorter: (a: CredentialType, b: CredentialType) => a.Name.localeCompare(b.Name),
    render: (name: string) => <div>{name}</div>,
  },
  {
    title: columnTooltip('Bearer Token', 'Authentication bearer token'),
    dataIndex: 'BearerToken',
    key: 'BearerToken',
    width: 160,
    ellipsis: true,
    render: (BearerToken: string) => (
      <span style={monoCellStyle} title={BearerToken}>
        <span style={monoValueStyle}>{BearerToken}</span>
      </span>
    ),
  },
  {
    title: columnTooltip('Active', 'Whether the credential is active'),
    dataIndex: 'Active',
    key: 'Active',
    width: 70,
    sorter: (a: CredentialType, b: CredentialType) => Number(b.Active) - Number(a.Active),
    render: (active: boolean) =>
      active ? (
        <CheckCircleFilled style={{ color: 'green' }} />
      ) : (
        <CloseCircleFilled style={{ color: 'red' }} />
      ),
  },
  {
    title: columnTooltip('Created UTC', 'Date and time of creation in UTC'),
    dataIndex: 'CreatedUtc',
    key: 'CreatedUtc',
    width: 150,
    sorter: (a: CredentialType, b: CredentialType) =>
      new Date(a.CreatedUtc).getTime() - new Date(b.CreatedUtc).getTime(),
    render: (CreatedUtc: string) => <div>{formatDateTime(CreatedUtc)}</div>,
    ellipsis: true,
  },
  {
    title: columnTooltip('Actions', 'Available operations'),
    key: 'actions',
    width: 70,
    render: (_: any, record: CredentialType) => {
      const items = [
        {
          key: 'edit',
          label: 'Edit',
          onClick: () => handleEdit(record),
        },
        {
          key: 'delete',
          label: 'Delete',
          onClick: () => handleDelete(record),
        },
        {
          icon: <CodeOutlined />,
          key: 'view-json',
          label: 'View JSON',
          onClick: () => handleViewJson?.(record),
        },
      ];
      return (
        <Dropdown menu={{ items }} trigger={['click']} placement="bottomRight">
          <LitegraphTooltip title="Actions">
            <Button
              type="text"
              icon={<MoreOutlined style={{ fontSize: '20px' }} />}
              role="credential-action-menu"
              style={{ fontSize: '16px' }}
            />
          </LitegraphTooltip>
        </Dropdown>
      );
    },
  },
];
