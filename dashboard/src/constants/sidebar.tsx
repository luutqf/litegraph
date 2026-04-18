import { paths } from './constant';
import {
  HomeOutlined,
  ShareAltOutlined,
  ApartmentOutlined,
  BranchesOutlined,
  CrownOutlined,
  TeamOutlined,
  TagOutlined,
  TagsOutlined,
  RadarChartOutlined,
  LockOutlined,
  SaveOutlined,
  HistoryOutlined,
  ApiOutlined,
  SafetyCertificateOutlined,
} from '@ant-design/icons';
import { MenuItemProps } from '@/components/menu-item/types';

export const tenantDashboardRoutes: MenuItemProps[] = [
  {
    key: '/',
    icon: <HomeOutlined />,
    label: 'Home',
    title: 'Dashboard overview',
    path: paths.dashboardHome,
  },
  {
    key: '/graphs',
    icon: <ShareAltOutlined />,
    label: 'Graphs',
    title: 'Manage graph containers',
    path: paths.graphs,
  },

  {
    key: '/nodes',
    icon: <ApartmentOutlined />,
    label: 'Nodes',
    title: 'Manage graph nodes',
    path: paths.nodes,
  },
  {
    key: '/edges',
    icon: <BranchesOutlined />,
    label: 'Edges',
    title: 'Manage graph edges',
    path: paths.edges,
  },
  {
    key: '/labels',
    icon: <TagOutlined />,
    label: 'Labels',
    title: 'Manage classification labels',
    path: paths.labels,
  },
  {
    key: '/tags',
    icon: <TagsOutlined />,
    label: 'Tags',
    title: 'Manage key-value tags',
    path: paths.tags,
  },
  {
    key: '/vectors',
    icon: <RadarChartOutlined />,
    label: 'Vectors',
    title: 'Manage vector embeddings',
    path: paths.vectors,
  },
  {
    key: '/request-history',
    icon: <HistoryOutlined />,
    label: 'Requests',
    title: 'HTTP request history',
    path: paths.requestHistory,
  },
  {
    key: '/api-explorer',
    icon: <ApiOutlined />,
    label: 'API Explorer',
    title: 'Explore and invoke API endpoints',
    path: paths.apiExplorer,
  },
];

export const adminDashboardRoutes: MenuItemProps[] = [
  {
    key: '/',
    icon: <CrownOutlined />,
    label: 'Tenants',
    title: 'Manage tenants',
    path: paths.adminDashboard,
  },
  {
    key: '/users',
    icon: <TeamOutlined />,
    label: 'Users',
    title: 'Manage user accounts',
    path: paths.users,
  },
  {
    key: '/credentials',
    icon: <LockOutlined />,
    label: 'Credentials',
    title: 'Manage API credentials',
    path: paths.credentials,
  },
  {
    key: '/authorization',
    icon: <SafetyCertificateOutlined />,
    label: 'Authorization',
    title: 'Manage roles and credential scopes',
    path: paths.authorization,
  },
  {
    key: '/backups',
    icon: <SaveOutlined />,
    label: 'Backups',
    title: 'Manage database backups',
    path: paths.backups,
  },
  {
    key: '/request-history',
    icon: <HistoryOutlined />,
    label: 'Requests',
    title: 'HTTP request history (all tenants)',
    path: paths.adminRequestHistory,
  },
  {
    key: '/api-explorer',
    icon: <ApiOutlined />,
    label: 'API Explorer',
    title: 'Explore and invoke API endpoints',
    path: paths.adminApiExplorer,
  },
];
