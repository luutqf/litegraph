import { DeleteOutlined, DownloadOutlined, MoreOutlined } from '@ant-design/icons';
import { formatDateTime } from '@/utils/dateUtils';
import { Button, Dropdown, MenuProps, TableProps } from 'antd';
import { formatBytes } from '@/utils/appUtils';
import { LoaderIcon } from 'react-hot-toast';
import { onNameFilter } from '@/constants/table';
import { FilterDropdownProps } from 'antd/es/table/interface';
import { BackupMetaData } from 'litegraphdb/dist/types/types';
import TableSearch from '@/components/table-search/TableSearch';
import { columnTooltip } from '@/utils/tooltipUtils';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';

const truncateCellStyle = {
  display: 'block',
  maxWidth: '100%',
  overflow: 'hidden',
  textOverflow: 'ellipsis',
  whiteSpace: 'nowrap',
} as const;

const monoTruncateCellStyle = {
  ...truncateCellStyle,
  fontFamily: 'monospace',
  fontSize: 12,
} as const;

export const tableColumns = (
  handleDelete: (backup: BackupMetaData) => void,
  handleDownload: (backup: BackupMetaData) => void,
  isDownloading: boolean
): TableProps<BackupMetaData>['columns'] => [
  {
    title: columnTooltip('Filename', 'Backup file name'),
    dataIndex: 'Filename',
    key: 'Filename',
    width: 220,
    ellipsis: true,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Filename" />
    ),
    sorter: (a: BackupMetaData, b: BackupMetaData) => a.Filename.localeCompare(b.Filename),
    onFilter: (value, record) => onNameFilter(value, record.Filename),
    render: (Filename: string) => (
      <span data-testid="backup-filename" style={truncateCellStyle} title={Filename}>
        {Filename}
      </span>
    ),
  },
  {
    title: columnTooltip('Size', 'Backup file size'),
    dataIndex: 'Length',
    key: 'Length',
    width: 100,
    render: (Length: number) => <div>{formatBytes(Length)}</div>,
  },
  {
    title: columnTooltip('SHA256', 'SHA-256 hash of the backup file'),
    dataIndex: 'SHA256Hash',
    key: 'SHA256Hash',
    width: 240,
    ellipsis: true,
    render: (hash: string) => (
      <span style={monoTruncateCellStyle} title={hash}>
        {hash}
      </span>
    ),
  },
  {
    title: columnTooltip('Create UTC', 'Date and time of creation in UTC'),
    dataIndex: 'CreatedUtc',
    key: 'CreatedUtc',
    width: 150,
    ellipsis: true,
    sorter: (a: BackupMetaData, b: BackupMetaData) =>
      new Date(a.CreatedUtc).getTime() - new Date(b.CreatedUtc).getTime(),
    render: (CreatedUtc: string) => <div>{formatDateTime(CreatedUtc)}</div>,
  },
  {
    title: columnTooltip('Actions', 'Available operations'),
    key: 'actions',
    width: 70,
    render: (_: any, record: BackupMetaData) => {
      const items: MenuProps['items'] = [
        {
          key: 'download',
          label: 'Download',
          onClick: () => handleDownload(record),
          icon: isDownloading ? <LoaderIcon /> : <DownloadOutlined />,
        },
        {
          key: 'delete',
          label: 'Delete',
          onClick: () => handleDelete(record),
          icon: <DeleteOutlined />,
        },
      ];
      return (
        <Dropdown menu={{ items }} trigger={['click']} placement="bottomRight">
          <LitegraphTooltip title="Actions">
            <Button
              type="text"
              icon={<MoreOutlined style={{ fontSize: '20px' }} />}
              role="backup-action-menu"
              style={{ fontSize: '16px' }}
            />
          </LitegraphTooltip>
        </Dropdown>
      );
    },
  },
];
