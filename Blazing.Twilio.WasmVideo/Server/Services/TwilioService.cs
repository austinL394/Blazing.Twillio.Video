﻿// Copyright (c) David Pine. All rights reserved.
// Licensed under the MIT License.

namespace Blazing.Twilio.WasmVideo.Server.Services;

public class TwilioService
{
    readonly TwilioSettings _twilioSettings;

    public TwilioService(MicrosoftOptions.IOptions<TwilioSettings> twilioOptions)
    {
        _twilioSettings =
            twilioOptions?.Value ??
            throw new ArgumentNullException(nameof(twilioOptions));

        TwilioClient.Init(
            _twilioSettings.ApiKey ?? "no-key",
            _twilioSettings.ApiSecret ?? "no-secret",
            _twilioSettings.AccountSid ?? "no-sid");
    }

    public TwilioJwt GetTwilioJwt(string? identity) =>
        new(Token: new Token(
            _twilioSettings.AccountSid,
            _twilioSettings.ApiKey,
            _twilioSettings.ApiSecret,
            identity ?? GetName(),
            grants: new HashSet<IGrant> { new VideoGrant() }).ToJwt());

    public async ValueTask<IEnumerable<RoomDetails>> GetAllRoomsAsync()
    {
        var rooms = await RoomResource.ReadAsync();
        var tasks = rooms.Select(
            room => GetRoomDetailsAsync(
                room,
                ParticipantResource.ReadAsync(
                    room.Sid,
                    ParticipantStatus.Connected)));

        return await Task.WhenAll(tasks);

        static async Task<RoomDetails> GetRoomDetailsAsync(
            RoomResource room,
            Task<ResourceSet<ParticipantResource>> participantTask)
        {
            var participants = await participantTask;
            return new RoomDetails
            {
                Name = room.UniqueName,
                MaxParticipants = room.MaxParticipants ?? 0,
                ParticipantCount = participants.Count()
            };
        }
    }

    #region Borrowed from https://github.com/twilio/video-quickstart-js/blob/1.x/server/randomname.js

    readonly string[] _adjectives =
    {
        "Abrasive", "Brash", "Callous", "Daft", "Eccentric", "Feisty", "Golden",
        "Holy", "Ignominious", "Luscious", "Mushy", "Nasty",
        "OldSchool", "Pompous", "Quiet", "Rowdy", "Sneaky", "Tawdry",
        "Unique", "Vivacious", "Wicked", "Xenophobic", "Yawning", "Zesty"
    };

    readonly string[] _firstNames =
    {
        "Anna", "Bobby", "Cameron", "Danny", "Emmett", "Frida", "Gracie", "Hannah",
        "Isaac", "Jenova", "Kendra", "Lando", "Mufasa", "Nate", "Owen", "Penny",
        "Quincy", "Roddy", "Samantha", "Tammy", "Ulysses", "Victoria", "Wendy",
        "Xander", "Yolanda", "Zelda"
    };

    readonly string[] _lastNames =
    {
        "Anchorage", "Berlin", "Cucamonga", "Davenport", "Essex", "Fresno",
        "Gunsight", "Hanover", "Indianapolis", "Jamestown", "Kane", "Liberty",
        "Minneapolis", "Nevis", "Oakland", "Portland", "Quantico", "Raleigh",
        "SaintPaul", "Tulsa", "Utica", "Vail", "Warsaw", "XiaoJin", "Yale",
        "Zimmerman"
    };

    string GetName() => $"{_adjectives.RandomElement()} {_firstNames.RandomElement()} {_lastNames.RandomElement()}";

    #endregion
}

static class StringArrayExtensions
{
    internal static string RandomElement(this IReadOnlyList<string> array) =>
        array[Random.Shared.Next(array.Count)];
}