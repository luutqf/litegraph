import React from 'react';
import { RootState } from '@/lib/store/store';
import { getFirstLetterOfTheWord, getUserName } from '@/utils/stringUtils';
import styles from './styles.module.scss';
import { useAppSelector } from '@/lib/store/hooks';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import LitegraphAvatar from '@/components/base/avatar/Avatar';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';

const LoggedUserInfo = () => {
  const user = useAppSelector((state: RootState) => state.liteGraph.user);
  const userName = getUserName(user);

  return (
    <LitegraphTooltip title={`Signed in as ${userName || 'user'}`}>
      <LitegraphFlex className={styles.container} gap={10} align="center">
        <LitegraphText className="ant-color-white" strong weight={400}>
          {userName}
        </LitegraphText>
        <LitegraphAvatar
          alt="User Profile"
          src={!userName && '/profile-pic.png'}
          size={'small'}
          style={{ background: 'primary' }}
        >
          {getFirstLetterOfTheWord(userName)}
        </LitegraphAvatar>
      </LitegraphFlex>
    </LitegraphTooltip>
  );
};

export default LoggedUserInfo;
