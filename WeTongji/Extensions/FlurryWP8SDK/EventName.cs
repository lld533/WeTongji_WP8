using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlurryWP8SDK.Models
{
    public enum EventName
    {
        ViewPersonalProfile,
        ViewMyFavorites,
        ClickReminder,
        ViewAgenda,
        SignIn,
        ViewCampusNews,
        ViewWeeklyStar,
        ClickAppbarRefreshButton,
        ClickAppBarHistoryButton,
        ClickAppBarSettingsMenuItem,
        ClickAppBarAboutMenuItem,
        ClickAppBarFeedbackMenuItem,
        ClickAppBarShareByEmailMenuItem,
        ClickSignOutMenuItem,
        ClickTheMostRecentMenuItem,
        ClickTheLatestMenuItem,
        ClickTheHottestMenuItem,
        ClickAppBarTodayButton,
        AddNewPersonalProfile,
        EditPersonalProfile,
        ChangeAvatar,
        SavePersonalProfile,
        ClickForgetPassword,
        ClickAppBarSignUpButton,
        ClickAppBarLikeButton,
        ClickAppBarUnlikeButton,
        ClickAppBarFavoriteButton,
        ClickAppBarUnfavoriteButton,
        ClickAppBarScheduleButton,
        ClickAppBarUnscheduleButton,
        SetConfirmToExit,
        SetAutoRefresh,
        ClearImageCache,
        CheckUpdate,
        ClickActivateButton,
        ClickSignUpNextStepButton,
        ClickDoneToSubmitSignUpInfo,
        ClickToBrowseTongjiMailPortAtSignUpThirdStep,
        ClickToGoBackAtSignUpThirdStep,
    }

    public enum ParameterName
    {
        ClickReminderParameter,
        SignInParameter,
        CampusNewsParameter,
        TapToViewWeeklyStarParameter,
        ClickAppBarRefreshButtonParameter,
        PersonalProfileParameter,
        LikeableParameter,
        FavorableParameter,
        ScheduleableParameter,
        Id,
        ResultState
    }

    public enum ParameterValue
    {
        Course,
        Exam,
        Activity,
        NoArrangement,

        AutoSignIn,
        SignIn,

        OfficialNote,
        GroupNotices,
        TongjiNews,
        Recommends,

        Picture,
        SeeMore,

        Feeds,
        Activities,
        CampusNews,
        WeeklyStar,

        Phone,
        QQ,
        Email,
        SinaWeibo,
    }
}
